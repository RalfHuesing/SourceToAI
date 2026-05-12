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

public class ConsoleOrchestratorTests
{
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
        var feedGenerator = new Mock<IFeedGenerator>(MockBehavior.Strict);
        var post = new Mock<IPostExportTask>(MockBehavior.Strict);

        var sut = new ConsoleOrchestrator(
            solutionDiscovery.Object,
            fileDiscovery.Object,
            feedGenerator.Object,
            new CsprojDependencyGraphMarkdownGenerator(),
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
        var feedGenerator = new Mock<IFeedGenerator>(MockBehavior.Strict);
        var post = new Mock<IPostExportTask>(MockBehavior.Strict);

        var sut = new ConsoleOrchestrator(
            solutionDiscovery.Object,
            fileDiscovery.Object,
            feedGenerator.Object,
            new CsprojDependencyGraphMarkdownGenerator(),
            TestAppSettingsFactory.Default(),
            [post.Object]);

        await sut.RunAsync(solution.Root, export.Root);

        Assert.Empty(Directory.GetDirectories(export.Root));
        post.Verify(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_writes_project_markdown_dependency_graph_and_runs_post_export_tasks()
    {
        using var export = new TempWorkspace();
        using var solution = new TempWorkspace();
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
            .Returns(ExtractionResult<List<string>>.Success([Path.Combine(solution.Root, "Proj1", "a.cs")]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(project2, It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([]));

        var feedGenerator = new Mock<IFeedGenerator>();
        feedGenerator
            .Setup(g => g.GenerateFeed("MySol", project1, It.IsAny<List<string>>()))
            .Returns(ExtractionResult<string>.Success("# feed"));

        var post = new Mock<IPostExportTask>();
        post.Setup(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = new ConsoleOrchestrator(
            solutionDiscovery.Object,
            fileDiscovery.Object,
            feedGenerator.Object,
            new CsprojDependencyGraphMarkdownGenerator(),
            TestAppSettingsFactory.Default(),
            [post.Object]);

        await sut.RunAsync(solution.Root, export.Root);

        var suffix = DateTime.Now.ToString("yyyyMMdd");
        var expectedFile = Path.Combine(export.Root, "MySol", $"MySol.Proj1-{suffix}.md");
        Assert.True(File.Exists(expectedFile));
        Assert.Equal("# feed", await File.ReadAllTextAsync(expectedFile, TestContext.Current.CancellationToken));

        var depGraphPath = Path.Combine(export.Root, "MySol", "multi-view", "dependency-graph.md");
        Assert.True(File.Exists(depGraphPath));
        var depGraph = await File.ReadAllTextAsync(depGraphPath, TestContext.Current.CancellationToken);
        Assert.Contains("## Proj1", depGraph, StringComparison.Ordinal);
        Assert.Contains("## Proj2", depGraph, StringComparison.Ordinal);
        Assert.Contains("Contoso.Core", depGraph, StringComparison.Ordinal);
        Assert.Contains("1.0.0", depGraph, StringComparison.Ordinal);
        Assert.Contains("Contoso.Tools", depGraph, StringComparison.Ordinal);
        Assert.Contains("Proj2/Proj2.csproj", depGraph, StringComparison.Ordinal);

        post.Verify(
            p => p.ExecuteAsync("MySol", Path.Combine(export.Root, "MySol")),
            Times.Once);
    }
}
