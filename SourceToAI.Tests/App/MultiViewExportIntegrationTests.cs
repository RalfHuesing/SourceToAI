using Microsoft.Extensions.DependencyInjection;
using Moq;
using SourceToAI.CLI.App;
using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Infrastructure;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Decompilation;
using SourceToAI.CLI.Services.Discovery;
using SourceToAI.CLI.Services.Export;
using SourceToAI.CLI.Services.Export.AiFeed;
using SourceToAI.CLI.Services.Integration;
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

    [Fact]
    public async Task RunAsync_multi_view_tree_matches_konzept_and_content_samples()
    {
        using var export = new TempWorkspace();
        using var solution = new TempWorkspace();
        using var multiViewSp = MultiViewExportTestHost.CreateServiceProvider();

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

            public record LibRowDto(int Id);
            """,
            TestContext.Current.CancellationToken);

        var proj2InternalOnly = Path.Combine(solution.Root, "Proj2", "FixtureInternalOnlyShell.cs");
        await File.WriteAllTextAsync(
            proj2InternalOnly,
            """
            namespace Fixture.Lib;

            internal static class FixtureInternalOnlyMarker { }
            """,
            TestContext.Current.CancellationToken);

        var proj3Csproj = Path.Combine(solution.Root, "Proj3", "Proj3.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(proj3Csproj)!);
        await File.WriteAllTextAsync(
            proj3Csproj,
            """<Project Sdk="Microsoft.NET.Sdk"></Project>""",
            TestContext.Current.CancellationToken);

        var proj3OnlyInternal = Path.Combine(solution.Root, "Proj3", "OnlyInternal.cs");
        await File.WriteAllTextAsync(
            proj3OnlyInternal,
            """
            namespace Fixture.Proj3Internal;

            internal static class Proj3InternalOnlyType { }
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
        var project3 = new ProjectDefinition("Proj3", proj3Csproj);

        var solutionDiscovery = new Mock<ISolutionDiscoveryService>();
        solutionDiscovery.Setup(s => s.GetSolutionName(solution.Root)).Returns(ExtractionResult<string>.Success(solutionName));
        solutionDiscovery
            .Setup(s => s.FindProjects(solution.Root))
            .Returns(ExtractionResult<List<ProjectDefinition>>.Success([project1, project2, project3]));

        var fileDiscovery = new Mock<IFileDiscoveryService>();
        fileDiscovery.Setup(f => f.FindSolutionDocs(solution.Root, It.IsAny<AppSettings>())).Returns(ExtractionResult<List<string>>.Success([]));
        fileDiscovery
            .Setup(f => f.FindUnmappedDirectories(solution.Root, It.IsAny<IReadOnlyList<ProjectDefinition>>(), It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<(string, List<string>)>>.Success([]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(project1, It.IsAny<string>(), It.IsAny<AppSettings>()))
            .Returns(
                ExtractionResult<List<string>>.Success(
                [
                    appCs,
                    modelsCs,
                    sidecarJson
                ]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(project2, It.IsAny<string>(), It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([libCs, proj2InternalOnly]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(project3, It.IsAny<string>(), It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([proj3OnlyInternal]));

        var post = new Mock<IPostExportTask>();
        post.Setup(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var assemblyDecompiler = new Mock<IAssemblyDecompilerService>(MockBehavior.Strict);

        var sut = new ConsoleOrchestrator(
            solutionDiscovery.Object,
            fileDiscovery.Object,
            assemblyDecompiler.Object,
            new CsprojDependencyGraphMarkdownGenerator(),
            multiViewSp.GetRequiredService<IMultiViewExportService>(),
            multiViewSp.GetRequiredService<IMultiViewReadmeMarkdownGenerator>(),
            TestAppSettingsFactory.Default(),
            [post.Object]);

        await File.WriteAllTextAsync(Path.Combine(export.Root, ".sta-marker"), "", TestContext.Current.CancellationToken);
        await sut.RunAsync([solution.Root], export.Root);

        var isolatedSolRoot = Path.Combine(export.Root, "Isolated", solutionName);
        Assert.True(Directory.Exists(isolatedSolRoot));

        Assert.True(File.Exists(Path.Combine(export.Root, "readme.md")));
        var globalReadme = await File.ReadAllTextAsync(
            Path.Combine(export.Root, "readme.md"),
            TestContext.Current.CancellationToken);
        Assert.Contains("Export-Verzeichnis", globalReadme, StringComparison.Ordinal);
        Assert.Contains("rg", globalReadme, StringComparison.Ordinal);
        Assert.Contains("Merged", globalReadme, StringComparison.Ordinal);
        Assert.DoesNotContain("full-source.md", globalReadme, StringComparison.OrdinalIgnoreCase);

        Assert.True(File.Exists(Path.Combine(isolatedSolRoot, "readme.md")));
        var isolatedReadme = await File.ReadAllTextAsync(
            Path.Combine(isolatedSolRoot, "readme.md"),
            TestContext.Current.CancellationToken);
        Assert.Contains("MANIFEST", isolatedReadme, StringComparison.Ordinal);
        Assert.Contains("CONTENT", isolatedReadme, StringComparison.Ordinal);
        Assert.Contains("pro Projekt", isolatedReadme, StringComparison.Ordinal);
        Assert.DoesNotContain("full-source.md", isolatedReadme, StringComparison.OrdinalIgnoreCase);
        
        Assert.True(File.Exists(Path.Combine(isolatedSolRoot, "dependency-graph.md")));
        var mergedRoot = Path.Combine(export.Root, "Merged");
        Assert.True(File.Exists(Path.Combine(mergedRoot, "complete", "FixtureSol.Proj1_complete.md")));
        Assert.True(File.Exists(Path.Combine(mergedRoot, "complete", "FixtureSol.Proj2_complete.md")));
        Assert.True(File.Exists(Path.Combine(mergedRoot, "complete", "FixtureSol.Proj3_complete.md")));
        Assert.True(File.Exists(Path.Combine(mergedRoot, "signatures-only", "FixtureSol.Proj1_signatures-only.md")));
        Assert.True(File.Exists(Path.Combine(mergedRoot, "signatures-only", "FixtureSol.Proj2_signatures-only.md")));
        Assert.True(File.Exists(Path.Combine(mergedRoot, "signatures-only", "FixtureSol.Proj3_signatures-only.md")));
        Assert.True(File.Exists(Path.Combine(mergedRoot, "public-only", "FixtureSol.Proj1_public-only.md")));
        Assert.True(File.Exists(Path.Combine(mergedRoot, "public-only", "FixtureSol.Proj2_public-only.md")));
        Assert.False(File.Exists(Path.Combine(mergedRoot, "public-only", "FixtureSol.Proj3_public-only.md")));
        Assert.True(File.Exists(Path.Combine(mergedRoot, "dto-only", "FixtureSol.Proj1_dto-only.md")));
        Assert.True(File.Exists(Path.Combine(mergedRoot, "dto-only", "FixtureSol.Proj2_dto-only.md")));
        Assert.False(File.Exists(Path.Combine(mergedRoot, "dto-only", "FixtureSol.Proj3_dto-only.md")));

        var signaturesMd = await File.ReadAllTextAsync(
            Path.Combine(mergedRoot, "signatures-only", "FixtureSol.Proj1_signatures-only.md"),
            TestContext.Current.CancellationToken)
            + await File.ReadAllTextAsync(
                Path.Combine(mergedRoot, "signatures-only", "FixtureSol.Proj2_signatures-only.md"),
                TestContext.Current.CancellationToken)
            + await File.ReadAllTextAsync(
                Path.Combine(mergedRoot, "signatures-only", "FixtureSol.Proj3_signatures-only.md"),
                TestContext.Current.CancellationToken);
        AiFeedExportIntegrationAsserts.AssertSignatureFencesParseWithoutSyntaxErrors(signaturesMd);
        Assert.Contains("ExprBackedProp", signaturesMd, StringComparison.Ordinal);
        Assert.DoesNotContain("=>", signaturesMd, StringComparison.Ordinal);

        var publicApi = await File.ReadAllTextAsync(
            Path.Combine(mergedRoot, "public-only", "FixtureSol.Proj1_public-only.md"),
            TestContext.Current.CancellationToken);
        Assert.DoesNotContain(PrivateFixtureMethodName, publicApi, StringComparison.Ordinal);
        Assert.Contains("PublicMethod", publicApi, StringComparison.Ordinal);

        var publicProj2 = await File.ReadAllTextAsync(
            Path.Combine(mergedRoot, "public-only", "FixtureSol.Proj2_public-only.md"),
            TestContext.Current.CancellationToken);
        Assert.Contains("LibMarker", publicProj2, StringComparison.Ordinal);
        Assert.DoesNotContain("FixtureInternalOnlyShell.cs", publicProj2, StringComparison.Ordinal);

        var completeProj2 = await File.ReadAllTextAsync(
            Path.Combine(mergedRoot, "complete", "FixtureSol.Proj2_complete.md"),
            TestContext.Current.CancellationToken);
        Assert.Contains("FixtureInternalOnlyMarker", completeProj2, StringComparison.Ordinal);

        var completeProj3 = await File.ReadAllTextAsync(
            Path.Combine(mergedRoot, "complete", "FixtureSol.Proj3_complete.md"),
            TestContext.Current.CancellationToken);
        Assert.Contains("Proj3InternalOnlyType", completeProj3, StringComparison.Ordinal);

        var modelsMd = await File.ReadAllTextAsync(
            Path.Combine(mergedRoot, "dto-only", "FixtureSol.Proj1_dto-only.md"),
            TestContext.Current.CancellationToken);
        Assert.Contains(FixtureDtoRecordName, modelsMd, StringComparison.Ordinal);
        Assert.Contains(FixtureEnumName, modelsMd, StringComparison.Ordinal);
        Assert.DoesNotContain("PublicMethod", modelsMd, StringComparison.Ordinal);

        var depGraph = await File.ReadAllTextAsync(
            Path.Combine(isolatedSolRoot, "dependency-graph.md"),
            TestContext.Current.CancellationToken);
        Assert.Contains(FixtureNuGetPackageId, depGraph, StringComparison.Ordinal);
        Assert.Contains("2.1.0", depGraph, StringComparison.Ordinal);

        var fullSource = await File.ReadAllTextAsync(
            Path.Combine(mergedRoot, "complete", "FixtureSol.Proj1_complete.md"),
            TestContext.Current.CancellationToken);
        Assert.Contains(PrivateFixtureMethodName, fullSource, StringComparison.Ordinal);
        Assert.Contains("sidecar.json", fullSource, StringComparison.Ordinal);
        Assert.Contains("{\"fixture\":true}", fullSource, StringComparison.Ordinal);

        post.Verify(p => p.ExecuteAsync(solutionName, isolatedSolRoot), Times.Once);
    }

    [Fact]
    public async Task RunAsync_with_splitting_active_generates_correct_file_names()
    {
        using var export = new TempWorkspace();
        using var solution = new TempWorkspace();

        var services = new ServiceCollection();
        var settings = TestAppSettingsFactory.Default();
        settings.MaxFileSizeKb = 1; // force splitting
        settings.MaxFileCount = 3; // allow up to 3 files
        services.AddSingleton(settings);
        services.AddSingleton<ICSharpDocumentLoader, CSharpDocumentLoader>();
        services.AddTransient<ProjectSplittingEngine>();
        services.AddViewGenerators();
        services.AddMarkdownProjectViewBuilders();
        services.AddSingleton<IAiFeedMarkdownComposer, AiFeedMarkdownComposer>();
        services.AddTransient<IMultiViewExportService, MultiViewExportService>();
        services.AddSingleton<IMultiViewReadmeMarkdownGenerator, MultiViewReadmeMarkdownGenerator>();
        using var multiViewSp = services.BuildServiceProvider();

        const string solutionName = "SplitSol";

        var projCsproj = Path.Combine(solution.Root, "Platform", "Platform.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(projCsproj)!);
        await File.WriteAllTextAsync(
            projCsproj,
            """<Project Sdk="Microsoft.NET.Sdk"></Project>""",
            TestContext.Current.CancellationToken);

        // Core namespace (no namespace) - Add padding to prevent sibling collapse
        await File.WriteAllTextAsync(
            Path.Combine(solution.Root, "Platform", "Program.cs"),
            "class Program { }" + new string('x', 500),
            TestContext.Current.CancellationToken);

        // Core namespace (starts with Platform.Core) - Add padding to prevent sibling collapse
        await File.WriteAllTextAsync(
            Path.Combine(solution.Root, "Platform", "Core.cs"),
            "namespace Platform.Core; public class CoreService { }" + new string('x', 500),
            TestContext.Current.CancellationToken);

        // Features namespace (starts with Platform.Features) - Add padding to prevent sibling collapse
        await File.WriteAllTextAsync(
            Path.Combine(solution.Root, "Platform", "Features.cs"),
            "namespace Platform.Features; public class FeatureService { }" + new string('x', 500),
            TestContext.Current.CancellationToken);

        var project = new ProjectDefinition("Platform", projCsproj);

        var solutionDiscovery = new Mock<ISolutionDiscoveryService>();
        solutionDiscovery.Setup(s => s.GetSolutionName(solution.Root)).Returns(ExtractionResult<string>.Success(solutionName));
        solutionDiscovery
            .Setup(s => s.FindProjects(solution.Root))
            .Returns(ExtractionResult<List<ProjectDefinition>>.Success([project]));

        var fileDiscovery = new Mock<IFileDiscoveryService>();
        fileDiscovery.Setup(f => f.FindSolutionDocs(solution.Root, It.IsAny<AppSettings>())).Returns(ExtractionResult<List<string>>.Success([]));
        fileDiscovery
            .Setup(f => f.FindUnmappedDirectories(solution.Root, It.IsAny<IReadOnlyList<ProjectDefinition>>(), It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<(string, List<string>)>>.Success([]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(project, It.IsAny<string>(), It.IsAny<AppSettings>()))
            .Returns(
                ExtractionResult<List<string>>.Success(
                [
                    Path.Combine(solution.Root, "Platform", "Program.cs"),
                    Path.Combine(solution.Root, "Platform", "Core.cs"),
                    Path.Combine(solution.Root, "Platform", "Features.cs")
                ]));

        var post = new Mock<IPostExportTask>();
        post.Setup(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var assemblyDecompiler = new Mock<IAssemblyDecompilerService>(MockBehavior.Strict);

        var sut = new ConsoleOrchestrator(
            solutionDiscovery.Object,
            fileDiscovery.Object,
            assemblyDecompiler.Object,
            new CsprojDependencyGraphMarkdownGenerator(),
            multiViewSp.GetRequiredService<IMultiViewExportService>(),
            multiViewSp.GetRequiredService<IMultiViewReadmeMarkdownGenerator>(),
            settings,
            [post.Object]);

        await File.WriteAllTextAsync(Path.Combine(export.Root, ".sta-marker"), "", TestContext.Current.CancellationToken);
        await sut.RunAsync([solution.Root], export.Root);

        var mergedRoot = Path.Combine(export.Root, "Merged", "complete");
        
        // Assert that the generated files are named with the sub-namespace suffix and the view key separated by underscores:
        // {SolutionName}.{ProjectName}_{SubNamespace}_{view}.md
        Assert.True(File.Exists(Path.Combine(mergedRoot, "SplitSol.Platform_Core_complete.md")), "Platform_Core file missing or incorrectly named");
        Assert.True(File.Exists(Path.Combine(mergedRoot, "SplitSol.Platform_Features_complete.md")), "Platform_Features file missing or incorrectly named");
    }
}
