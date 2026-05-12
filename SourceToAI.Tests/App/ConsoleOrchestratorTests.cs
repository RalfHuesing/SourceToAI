using Microsoft.Extensions.DependencyInjection;
using Moq;
using SourceToAI.CLI.App;
using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Infrastructure;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Discovery;
using SourceToAI.CLI.Services.Export;
using SourceToAI.CLI.Services.Export.AiFeed;
using SourceToAI.CLI.Services.Integration;
using SourceToAI.CLI.Services.IO;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.App;

public class ConsoleOrchestratorTests
{
    private static ServiceProvider CreateMultiViewServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileReader, PhysicalFileReader>();
        services.AddSingleton<ICSharpDocumentLoader, CSharpDocumentLoader>();
        services.AddTransient<IFileTypeService, FileTypeService>();
        services.AddViewGenerators();
        services.AddMarkdownProjectViewBuilders();
        services.AddSingleton<IAiFeedMarkdownComposer, AiFeedMarkdownComposer>();
        services.AddTransient<IMultiViewExportService, MultiViewExportService>();
        services.AddSingleton<IMultiViewReadmeMarkdownGenerator, MultiViewReadmeMarkdownGenerator>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task RunAsync_early_exit_when_solution_name_fails_does_not_create_output()
    {
        using var export = new TempWorkspace();
        using var solution = new TempWorkspace();
        var solutionDiscovery = new Mock<ISolutionDiscoveryService>(MockBehavior.Strict);
        solutionDiscovery
            .Setup(s => s.GetSolutionName(solution.Root))
            .Returns(ExtractionResult<string>.Failure("no solution"));

        var fileDiscovery = new Mock<IFileDiscoveryService>(MockBehavior.Strict);
        var multiView = new Mock<IMultiViewExportService>(MockBehavior.Strict);
        var readme = new Mock<IMultiViewReadmeMarkdownGenerator>(MockBehavior.Strict);
        var post = new Mock<IPostExportTask>(MockBehavior.Strict);

        var sut = new ConsoleOrchestrator(
            solutionDiscovery.Object,
            fileDiscovery.Object,
            new CsprojDependencyGraphMarkdownGenerator(),
            multiView.Object,
            readme.Object,
            TestAppSettingsFactory.Default(),
            [post.Object]);

        await sut.RunAsync(solution.Root, export.Root);

        Assert.Empty(Directory.GetDirectories(export.Root));
        post.Verify(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_early_exit_when_find_projects_fails_does_not_create_output()
    {
        using var export = new TempWorkspace();
        using var solution = new TempWorkspace();
        var solutionDiscovery = new Mock<ISolutionDiscoveryService>(MockBehavior.Strict);
        solutionDiscovery
            .Setup(s => s.GetSolutionName(solution.Root))
            .Returns(ExtractionResult<string>.Success("MySol"));
        solutionDiscovery
            .Setup(s => s.FindProjects(solution.Root))
            .Returns(ExtractionResult<List<ProjectDefinition>>.Failure("no projects"));

        var fileDiscovery = new Mock<IFileDiscoveryService>(MockBehavior.Strict);
        var multiView = new Mock<IMultiViewExportService>(MockBehavior.Strict);
        var readme = new Mock<IMultiViewReadmeMarkdownGenerator>(MockBehavior.Strict);
        var post = new Mock<IPostExportTask>(MockBehavior.Strict);

        var sut = new ConsoleOrchestrator(
            solutionDiscovery.Object,
            fileDiscovery.Object,
            new CsprojDependencyGraphMarkdownGenerator(),
            multiView.Object,
            readme.Object,
            TestAppSettingsFactory.Default(),
            [post.Object]);

        await sut.RunAsync(solution.Root, export.Root);

        Assert.Empty(Directory.GetDirectories(export.Root));
        post.Verify(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_writes_multi_view_tree_readme_dependency_graph_and_post_export_tasks()
    {
        using var export = new TempWorkspace();
        using var solution = new TempWorkspace();
        using var multiViewSp = CreateMultiViewServiceProvider();

        var proj2Path = Path.Combine(solution.Root, "Proj2", "Proj2.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(proj2Path)!);
        await File.WriteAllTextAsync(
            proj2Path,
            """<Project Sdk="Microsoft.NET.Sdk"></Project>""",
            TestContext.Current.CancellationToken);

        var projPath = Path.Combine(solution.Root, "Proj1", "Proj1.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(projPath)!);
        await File.WriteAllTextAsync(
            projPath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Contoso.Core" Version="1.0.0" />
                <PackageReference Include="Contoso.Tools" />
                <ProjectReference Include="..\Proj2\Proj2.csproj" />
              </ItemGroup>
            </Project>
            """,
            TestContext.Current.CancellationToken);

        var csPath = Path.Combine(solution.Root, "Proj1", "Sample.cs");
        await File.WriteAllTextAsync(
            csPath,
            """
            namespace Demo;

            public class Sample { }

            public record UnitDto(int Id);
            """,
            TestContext.Current.CancellationToken);

        var project1 = new ProjectDefinition("Proj1", projPath);
        var project2 = new ProjectDefinition("Proj2", proj2Path);
        var solutionDiscovery = new Mock<ISolutionDiscoveryService>();
        solutionDiscovery
            .Setup(s => s.GetSolutionName(solution.Root))
            .Returns(ExtractionResult<string>.Success("MySol"));
        solutionDiscovery
            .Setup(s => s.FindProjects(solution.Root))
            .Returns(ExtractionResult<List<ProjectDefinition>>.Success([project1, project2]));

        var fileDiscovery = new Mock<IFileDiscoveryService>();
        fileDiscovery
            .Setup(f => f.FindSolutionDocs(solution.Root, It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(project1, It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([csPath]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(project2, It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([]));

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

        var outRoot = Path.Combine(export.Root, "MySol");
        Assert.True(Directory.Exists(outRoot));

        var readmePath = Path.Combine(outRoot, "readme.md");
        Assert.True(File.Exists(readmePath));
        var readmeText = await File.ReadAllTextAsync(readmePath, TestContext.Current.CancellationToken);
        var folderName = new DirectoryInfo(Path.TrimEndingDirectorySeparator(solution.Root)).Name;
        Assert.Contains(folderName, readmeText, StringComparison.Ordinal);
        Assert.Matches(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z", readmeText);
        Assert.Contains("MANIFEST", readmeText, StringComparison.Ordinal);
        Assert.Contains("pro Projekt", readmeText, StringComparison.Ordinal);
        Assert.DoesNotContain("full-source.md", readmeText, StringComparison.OrdinalIgnoreCase);

        var depGraphPath = Path.Combine(outRoot, "dependency-graph.md");
        Assert.True(File.Exists(depGraphPath));
        var depGraph = await File.ReadAllTextAsync(depGraphPath, TestContext.Current.CancellationToken);
        Assert.Contains("## Proj1", depGraph, StringComparison.Ordinal);
        Assert.Contains("## Proj2", depGraph, StringComparison.Ordinal);
        Assert.Contains("Contoso.Core", depGraph, StringComparison.Ordinal);
        Assert.Contains("1.0.0", depGraph, StringComparison.Ordinal);
        Assert.Contains("Contoso.Tools", depGraph, StringComparison.Ordinal);
        Assert.Contains("Proj2/Proj2.csproj", depGraph, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(outRoot, "complete", "MySol.Proj1.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "signatures-only", "MySol.Proj1.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "public-only", "MySol.Proj1.md")));
        Assert.True(File.Exists(Path.Combine(outRoot, "dto-only", "MySol.Proj1.md")));

        var dtoMd = await File.ReadAllTextAsync(
            Path.Combine(outRoot, "dto-only", "MySol.Proj1.md"),
            TestContext.Current.CancellationToken);
        Assert.Contains("UnitDto", dtoMd, StringComparison.Ordinal);

        var fullSource = await File.ReadAllTextAsync(Path.Combine(outRoot, "complete", "MySol.Proj1.md"), TestContext.Current.CancellationToken);
        Assert.Contains("# AI FEED:", fullSource, StringComparison.Ordinal);
        Assert.Contains("Sample.cs", fullSource, StringComparison.Ordinal);
        Assert.Matches(@"### \[\d+\] .*Sample\.cs", fullSource);
        Assert.Contains("public class Sample", fullSource, StringComparison.Ordinal);
        Assert.Contains("UnitDto", fullSource, StringComparison.Ordinal);

        post.Verify(
            p => p.ExecuteAsync("MySol", Path.Combine(export.Root, "MySol")),
            Times.Once);
    }
}
