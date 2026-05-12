using Moq;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Discovery;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.Discovery;

public class FileDiscoveryServiceTests
{
    private static FileDiscoveryService CreateSut(IDirectoryEnumerator? enumerator = null) =>
        new(enumerator ?? new DefaultDirectoryEnumerator());

    private static bool CollectionContainsPath(IReadOnlyList<string> paths, string expected) =>
        paths.Any(p => string.Equals(Path.GetFullPath(p), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void FindSolutionDocs_collects_readme_cursor_rules_and_github_workflows()
    {
        using var ws = new TempWorkspace();
        var readme = ws.WriteFile("README.md", "# r");
        ws.WriteFile(".cursor/rules/a.mdc", "rule");
        var workflow = ws.WriteFile(".github/workflows/ci.yml", "on: push");
        var settings = TestAppSettingsFactory.Default();

        var result = CreateSut().FindSolutionDocs(ws.Root, settings);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Value);
        Assert.True(CollectionContainsPath(result.Value, readme));
        Assert.True(CollectionContainsPath(result.Value, workflow));
        Assert.True(CollectionContainsPath(result.Value, Path.Combine(ws.Root, ".cursor", "rules", "a.mdc")));
    }

    [Fact]
    public void FindFilesForProject_respects_excluded_directories()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("App/App.csproj", "<Project></Project>");
        var ok = ws.WriteFile("App/Good.cs", "// ok");
        ws.WriteFile("App/bin/Hidden.cs", "// should not appear");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "App", "App.csproj"));
        var settings = TestAppSettingsFactory.Default();

        var result = CreateSut().FindFilesForProject(project, settings);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Value);
        Assert.True(CollectionContainsPath(result.Value, ok));
        var hiddenInBin = result.Value!.Where(p =>
            p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            && p.EndsWith("Hidden.cs", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Empty(hiddenInBin);
    }

    [Fact]
    public void FindFilesForProject_nonexistent_root_fails()
    {
        var project = new ProjectDefinition(
            "Ghost",
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing", "x.csproj"));
        var settings = TestAppSettingsFactory.Default();

        var result = CreateSut().FindFilesForProject(project, settings);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void FindFilesForProject_skips_subdirectory_when_enumeration_throws_but_keeps_other_files()
    {
        using var ws = new TempWorkspace();
        var appRoot = Path.Combine(ws.Root, "App");
        Directory.CreateDirectory(appRoot);
        var csproj = Path.Combine(appRoot, "App.csproj");
        File.WriteAllText(csproj, "<Project></Project>");
        var goodCs = Path.Combine(appRoot, "Good.cs");
        File.WriteAllText(goodCs, "// ok");
        var blocked = Path.Combine(appRoot, "Blocked");
        Directory.CreateDirectory(blocked);

        var mock = new Mock<IDirectoryEnumerator>(MockBehavior.Strict);
        mock.Setup(e => e.EnumerateFiles(appRoot)).Returns([goodCs]);
        mock.Setup(e => e.EnumerateDirectories(appRoot)).Returns([blocked]);
        mock.Setup(e => e.EnumerateFiles(blocked)).Throws<UnauthorizedAccessException>();

        var project = new ProjectDefinition("App", csproj);
        var settings = TestAppSettingsFactory.Default();

        var result = CreateSut(mock.Object).FindFilesForProject(project, settings);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Value);
        Assert.True(CollectionContainsPath(result.Value!, goodCs));
        Assert.NotNull(result.Warnings);
        Assert.NotEmpty(result.Warnings);
        mock.Verify(e => e.EnumerateDirectories(blocked), Times.Never);
    }
}
