using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SourceToAI.CLI.App;
using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Infrastructure;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Discovery;
using SourceToAI.CLI.Services.Export;
using SourceToAI.CLI.Services.Integration;
using SourceToAI.CLI.Services.IO;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.App;

/// <summary>
/// End-to-End über <see cref="ConsoleOrchestrator"/> mit echter Multi-View-Pipeline und temporärer Mini-Solution.
/// </summary>
public sealed class MultiViewExportIntegrationTests
{
    public const string PrivateFixtureMethodName = "SecretPrivateMethodNameFromFixture";
    public const string FixtureDtoRecordName = "IntegrationFixtureOrderDto";
    public const string FixtureEnumName = "IntegrationFixtureStatus";
    public const string FixtureNuGetPackageId = "IntegrationFixture.NuGetPkg";

    private static ServiceProvider CreateMultiViewServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileReader, PhysicalFileReader>();
        services.AddTransient<ICSharpDocumentLoader, CSharpDocumentLoader>();
        services.AddTransient<IFileTypeService, FileTypeService>();
        services.AddViewGenerators();
        services.AddMarkdownProjectViewBuilders();
        services.AddTransient<IMultiViewExportService, MultiViewExportService>();
        services.AddSingleton<IMultiViewReadmeMarkdownGenerator, MultiViewReadmeMarkdownGenerator>();
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Extrahiert Inhalte aus <c>```…csharp</c>-Blöcken (variable Fence-Länge wie in der CLI).
    /// </summary>
    internal static IEnumerable<string> ExtractCSharpFenceContents(string markdown)
    {
        using var reader = new StringReader(markdown);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var m = Regex.Match(line, @"^(?<fence>`{3,})csharp\s*$");
            if (!m.Success)
                continue;

            var fence = m.Groups["fence"].Value;
            var sb = new StringBuilder();
            while ((line = reader.ReadLine()) != null)
            {
                if (line == fence)
                {
                    var code = sb.ToString().TrimEnd('\r', '\n');
                    if (!string.IsNullOrWhiteSpace(code))
                        yield return code;
                    break;
                }

                sb.AppendLine(line);
            }
        }
    }

    private static void AssertAllSignatureBlocksParseWithoutErrors(string signaturesMarkdown)
    {
        var blocks = ExtractCSharpFenceContents(signaturesMarkdown).ToList();
        Assert.NotEmpty(blocks);
        foreach (var code in blocks)
        {
            var tree = CSharpSyntaxTree.ParseText(
                code,
                CSharpParseOptions.Default,
                path: "signatures-fragment.cs",
                encoding: Encoding.UTF8);
            var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
        }
    }

    [Fact]
    public async Task RunAsync_multi_view_tree_matches_konzept_and_content_samples()
    {
        using var export = new TempWorkspace();
        using var solution = new TempWorkspace();
        using var multiViewSp = CreateMultiViewServiceProvider();

        const string solutionName = "FixtureSol";

        var proj2Csproj = Path.Combine(solution.Root, "Proj2", "Proj2.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(proj2Csproj)!);
        await File.WriteAllTextAsync(
            proj2Csproj,
            """<Project Sdk="Microsoft.NET.Sdk"></Project>""",
            TestContext.Current.CancellationToken);

        var libCs = Path.Combine(solution.Root, "Proj2", "LibTypes.cs");
        await File.WriteAllTextAsync(
            libCs,
            """
            namespace Fixture.Lib;

            public static class LibMarker
            {
                public static int Value => 1;
            }
            """,
            TestContext.Current.CancellationToken);

        var proj1Csproj = Path.Combine(solution.Root, "Proj1", "Proj1.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(proj1Csproj)!);
        await File.WriteAllTextAsync(
            proj1Csproj,
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="{FixtureNuGetPackageId}" Version="2.1.0" />
                <ProjectReference Include="..\Proj2\Proj2.csproj" />
              </ItemGroup>
            </Project>
            """,
            TestContext.Current.CancellationToken);

        var appCs = Path.Combine(solution.Root, "Proj1", "App.cs");
        await File.WriteAllTextAsync(
            appCs,
            $$"""
            namespace Fixture.App;

            public class ServiceWithSecrets
            {
                public int ExprBackedProp => 42;

                public void PublicMethod() { }

                private void {{PrivateFixtureMethodName}}() { }

                private int PrivateExpr => 99;
            }
            """,
            TestContext.Current.CancellationToken);

        var modelsCs = Path.Combine(solution.Root, "Proj1", "Models.cs");
        await File.WriteAllTextAsync(
            modelsCs,
            $$"""
            namespace Fixture.Models;

            public enum {{FixtureEnumName}}
            {
                Pending,
                Done
            }

            public record {{FixtureDtoRecordName}}(System.Guid Id, string Label);
            """,
            TestContext.Current.CancellationToken);

        var sidecarJson = Path.Combine(solution.Root, "Proj1", "sidecar.json");
        await File.WriteAllTextAsync(sidecarJson, """{"fixture":true}""", TestContext.Current.CancellationToken);

        var project1 = new ProjectDefinition("Proj1", proj1Csproj);
        var project2 = new ProjectDefinition("Proj2", proj2Csproj);

        var solutionDiscovery = new Mock<ISolutionDiscoveryService>();
        solutionDiscovery.Setup(s => s.GetSolutionName(solution.Root)).Returns(ExtractionResult<string>.Success(solutionName));
        solutionDiscovery
            .Setup(s => s.FindProjects(solution.Root))
            .Returns(ExtractionResult<List<ProjectDefinition>>.Success([project1, project2]));

        var fileDiscovery = new Mock<IFileDiscoveryService>();
        fileDiscovery.Setup(f => f.FindSolutionDocs(solution.Root, It.IsAny<AppSettings>())).Returns(ExtractionResult<List<string>>.Success([]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(project1, It.IsAny<AppSettings>()))
            .Returns(
                ExtractionResult<List<string>>.Success(
                [
                    appCs,
                    modelsCs,
                    sidecarJson
                ]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(project2, It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([libCs]));

        var post = new Mock<IPostExportTask>();
        post.Setup(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = new ConsoleOrchestrator(
            solutionDiscovery.Object,
            fileDiscovery.Object,
            new CsprojDependencyGraphMarkdownGenerator(),
            multiViewSp.GetRequiredService<IMultiViewExportService>(),
            multiViewSp.GetRequiredService<IMultiViewReadmeMarkdownGenerator>(),
            TestAppSettingsFactory.Default(),
            [post.Object]);

        await sut.RunAsync(solution.Root, export.Root);

        var outRoot = Path.Combine(export.Root, solutionName);
        Assert.True(Directory.Exists(outRoot));

        // konzept.md Abschnitt 2 — alle Pfade relativ zu {export}/{solutionName}
        Assert.True(File.Exists(Path.Combine(outRoot, "readme.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "dependency-graph.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "complete", "FixtureSol.Proj1.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "complete", "FixtureSol.Proj2.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "signatures-only", "FixtureSol.Proj1.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "signatures-only", "FixtureSol.Proj2.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "public-only", "FixtureSol.Proj1.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "public-only", "FixtureSol.Proj2.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "dto-only", "FixtureSol.Proj1.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "dto-only", "FixtureSol.Proj2.md")));

        var signaturesMd = await File.ReadAllTextAsync(
            Path.Combine(outRoot, "signatures-only", "FixtureSol.Proj1.md"),
            TestContext.Current.CancellationToken)
            + await File.ReadAllTextAsync(
                Path.Combine(outRoot, "signatures-only", "FixtureSol.Proj2.md"),
                TestContext.Current.CancellationToken);
        AssertAllSignatureBlocksParseWithoutErrors(signaturesMd);
        Assert.Contains("ExprBackedProp", signaturesMd, StringComparison.Ordinal);
        Assert.DoesNotContain("=>", signaturesMd, StringComparison.Ordinal);

        var publicApi = await File.ReadAllTextAsync(
            Path.Combine(outRoot, "public-only", "FixtureSol.Proj1.md"),
            TestContext.Current.CancellationToken);
        Assert.DoesNotContain(PrivateFixtureMethodName, publicApi, StringComparison.Ordinal);
        Assert.Contains("PublicMethod", publicApi, StringComparison.Ordinal);

        var modelsMd = await File.ReadAllTextAsync(
            Path.Combine(outRoot, "dto-only", "FixtureSol.Proj1.md"),
            TestContext.Current.CancellationToken);
        Assert.Contains(FixtureDtoRecordName, modelsMd, StringComparison.Ordinal);
        Assert.Contains(FixtureEnumName, modelsMd, StringComparison.Ordinal);
        Assert.DoesNotContain("PublicMethod", modelsMd, StringComparison.Ordinal);

        var depGraph = await File.ReadAllTextAsync(
            Path.Combine(outRoot, "dependency-graph.md"),
            TestContext.Current.CancellationToken);
        Assert.Contains(FixtureNuGetPackageId, depGraph, StringComparison.Ordinal);
        Assert.Contains("2.1.0", depGraph, StringComparison.Ordinal);

        var fullSource = await File.ReadAllTextAsync(
            Path.Combine(outRoot, "complete", "FixtureSol.Proj1.md"),
            TestContext.Current.CancellationToken);
        Assert.Contains(PrivateFixtureMethodName, fullSource, StringComparison.Ordinal);
        Assert.Contains("sidecar.json", fullSource, StringComparison.Ordinal);
        Assert.Contains("{\"fixture\":true}", fullSource, StringComparison.Ordinal);

        post.Verify(p => p.ExecuteAsync(solutionName, outRoot), Times.Once);
    }
}
