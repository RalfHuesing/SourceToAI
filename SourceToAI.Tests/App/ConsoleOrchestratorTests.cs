using Moq;
using SourceToAI.CLI.App;
using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Discovery;
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
            TestAppSettingsFactory.Default(),
            [post.Object]);

        await sut.RunAsync(solution.Root, export.Root);

        Assert.Empty(Directory.GetDirectories(export.Root));
        post.Verify(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_writes_project_markdown_and_runs_post_export_tasks()
    {
        using var export = new TempWorkspace();
        using var solution = new TempWorkspace();
        var projPath = Path.Combine(solution.Root, "Proj1", "Proj1.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(projPath)!);
        await File.WriteAllTextAsync(projPath, "<Project></Project>", TestContext.Current.CancellationToken);

        var project = new ProjectDefinition("Proj1", projPath);
        var solutionDiscovery = new Mock<ISolutionDiscoveryService>();
        solutionDiscovery
            .Setup(s => s.GetSolutionName(solution.Root))
            .Returns(ExtractionResult<string>.Success("MySol"));
        solutionDiscovery
            .Setup(s => s.FindProjects(solution.Root))
            .Returns(ExtractionResult<List<ProjectDefinition>>.Success([project]));

        var fileDiscovery = new Mock<IFileDiscoveryService>();
        fileDiscovery
            .Setup(f => f.FindSolutionDocs(solution.Root, It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(project, It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([Path.Combine(solution.Root, "Proj1", "a.cs")]));

        var feedGenerator = new Mock<IFeedGenerator>();
        feedGenerator
            .Setup(g => g.GenerateFeed("MySol", project, It.IsAny<List<string>>()))
            .Returns(ExtractionResult<string>.Success("# feed"));

        var post = new Mock<IPostExportTask>();
        post.Setup(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = new ConsoleOrchestrator(
            solutionDiscovery.Object,
            fileDiscovery.Object,
            feedGenerator.Object,
            TestAppSettingsFactory.Default(),
            [post.Object]);

        await sut.RunAsync(solution.Root, export.Root);

        var suffix = DateTime.Now.ToString("yyyyMMdd");
        var expectedFile = Path.Combine(export.Root, "MySol", $"MySol.Proj1-{suffix}.md");
        Assert.True(File.Exists(expectedFile));
        Assert.Equal("# feed", await File.ReadAllTextAsync(expectedFile, TestContext.Current.CancellationToken));

        post.Verify(
            p => p.ExecuteAsync("MySol", Path.Combine(export.Root, "MySol")),
            Times.Once);
    }
}
