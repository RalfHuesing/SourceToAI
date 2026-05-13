using Microsoft.Extensions.DependencyInjection;
using Moq;
using SourceToAI.CLI.App;
using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Discovery;
using SourceToAI.CLI.Services.Export;
using SourceToAI.CLI.Services.Integration;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.App;

/// <summary>
/// E2E-Verifikation der pro-Projekt-AI-Feed-Dateien: Frontmatter, MANIFEST/CONTENT-Konsistenz,
/// Namensschema <c>Solution.Project.md</c>, View-Filter (<c>public-only</c>).
/// </summary>
public sealed class AiFeedProjectGranularityIntegrationTests
{
    /// <remarks>
    /// Parse Once, Rewrite Multiple: dieselbe <see cref="ICSharpDocumentLoader"/>-Pipeline wie Produktion;
    /// kein zusätzlicher Mess-Hook — Verhalten wird mit dem bestehenden Orchestrierungsweg abgesichert.
    /// </remarks>
    [Fact]
    public async Task RunAsync_two_projects_emit_per_view_files_with_manifest_content_invariants()
    {
        using var export = new TempWorkspace();
        using var solution = new TempWorkspace();
        using var multiViewSp = MultiViewExportTestHost.CreateServiceProvider();

        const string solutionName = "GranSol";

        var projBCsproj = Path.Combine(solution.Root, "ProjB", "ProjB.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(projBCsproj)!);
        await File.WriteAllTextAsync(
            projBCsproj,
            """<Project Sdk="Microsoft.NET.Sdk"></Project>""",
            TestContext.Current.CancellationToken);

        var libCs = Path.Combine(solution.Root, "ProjB", "Lib.cs");
        await File.WriteAllTextAsync(
            libCs,
            """
            namespace Gran.B;

            public static class LibApi
            {
                public static int N => 7;
            }
            """,
            TestContext.Current.CancellationToken);

        var projACsproj = Path.Combine(solution.Root, "ProjA", "ProjA.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(projACsproj)!);
        await File.WriteAllTextAsync(
            projACsproj,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\ProjB\ProjB.csproj" />
              </ItemGroup>
            </Project>
            """,
            TestContext.Current.CancellationToken);

        var appCs = Path.Combine(solution.Root, "ProjA", "App.cs");
        await File.WriteAllTextAsync(
            appCs,
            $$"""
            namespace Gran.A;

            public class AlphaService
            {
                public void Visible() { }

                private void {{MultiViewExportIntegrationTests.PrivateFixtureMethodName}}() { }
            }
            """,
            TestContext.Current.CancellationToken);

        var noteMd = Path.Combine(solution.Root, "ProjA", "Note.md");
        await File.WriteAllTextAsync(
            noteMd,
            "# Hinweis\n\nNur Dokumentation.",
            TestContext.Current.CancellationToken);

        var projectA = new ProjectDefinition("ProjA", projACsproj);
        var projectB = new ProjectDefinition("ProjB", projBCsproj);

        var solutionDiscovery = new Mock<ISolutionDiscoveryService>();
        solutionDiscovery.Setup(s => s.GetSolutionName(solution.Root)).Returns(ExtractionResult<string>.Success(solutionName));
        solutionDiscovery
            .Setup(s => s.FindProjects(solution.Root))
            .Returns(ExtractionResult<List<ProjectDefinition>>.Success([projectA, projectB]));

        var fileDiscovery = new Mock<IFileDiscoveryService>();
        fileDiscovery.Setup(f => f.FindSolutionDocs(solution.Root, It.IsAny<AppSettings>())).Returns(ExtractionResult<List<string>>.Success([]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(projectA, It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([appCs, noteMd]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(projectB, It.IsAny<AppSettings>()))
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

        await sut.RunAsync([solution.Root], export.Root);

        var outRoot = Path.Combine(export.Root, solutionName);
        Assert.True(Directory.Exists(outRoot));

        // Eine Markdown-Datei pro Projekt und View (Präfix Solution, Suffix .md)
        Assert.True(File.Exists(Path.Combine(outRoot, "complete", "GranSol.ProjA.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "complete", "GranSol.ProjB.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "public-only", "GranSol.ProjA.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "public-only", "GranSol.ProjB.md")));

        var completeA = await File.ReadAllTextAsync(
            Path.Combine(outRoot, "complete", "GranSol.ProjA.md"),
            TestContext.Current.CancellationToken);
        var completeB = await File.ReadAllTextAsync(
            Path.Combine(outRoot, "complete", "GranSol.ProjB.md"),
            TestContext.Current.CancellationToken);
        var publicA = await File.ReadAllTextAsync(
            Path.Combine(outRoot, "public-only", "GranSol.ProjA.md"),
            TestContext.Current.CancellationToken);
        var publicB = await File.ReadAllTextAsync(
            Path.Combine(outRoot, "public-only", "GranSol.ProjB.md"),
            TestContext.Current.CancellationToken);

        AiFeedExportIntegrationAsserts.AssertAiFeedStructuralInvariants(completeA);
        AiFeedExportIntegrationAsserts.AssertAiFeedStructuralInvariants(completeB);
        AiFeedExportIntegrationAsserts.AssertAiFeedStructuralInvariants(publicA);
        AiFeedExportIntegrationAsserts.AssertAiFeedStructuralInvariants(publicB);

        Assert.Contains(MultiViewExportIntegrationTests.PrivateFixtureMethodName, completeA, StringComparison.Ordinal);
        Assert.DoesNotContain(MultiViewExportIntegrationTests.PrivateFixtureMethodName, publicA, StringComparison.Ordinal);
        Assert.Contains("Visible", publicA, StringComparison.Ordinal);

        var sigA = await File.ReadAllTextAsync(
            Path.Combine(outRoot, "signatures-only", "GranSol.ProjA.md"),
            TestContext.Current.CancellationToken);
        var sigB = await File.ReadAllTextAsync(
            Path.Combine(outRoot, "signatures-only", "GranSol.ProjB.md"),
            TestContext.Current.CancellationToken);
        AiFeedExportIntegrationAsserts.AssertSignatureFencesParseWithoutSyntaxErrors(sigA + sigB);

        post.Verify(p => p.ExecuteAsync(solutionName, outRoot), Times.Once);
    }
}
