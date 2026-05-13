using SourceToAI.CLI.Services.Discovery;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.Discovery;

public class SolutionDiscoveryServiceTests
{
    private readonly SolutionDiscoveryService _sut = new();

    [Fact]
    public void GetSolutionName_without_sln_uses_directory_name()
    {
        using var ws = new TempWorkspace();

        var result = _sut.GetSolutionName(ws.Root);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(new DirectoryInfo(ws.Root).Name, result.Value);
    }

    [Fact]
    public void GetSolutionName_prefers_first_sln_in_root()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("Alpha.sln", "");
        ws.WriteFile("Beta.sln", "");

        var result = _sut.GetSolutionName(ws.Root);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("Alpha", result.Value);
    }

    [Fact]
    public void GetSolutionName_without_sln_under_decompile_folder_uses_parent_directory_name()
    {
        using var ws = new TempWorkspace();
        var decompileDir = Path.Combine(ws.Root, "MyAssembly", "decompile");
        Directory.CreateDirectory(decompileDir);

        var result = _sut.GetSolutionName(decompileDir);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("MyAssembly", result.Value);
    }

    [Fact]
    public void GetSolutionName_decompile_segment_is_case_insensitive_for_parent_resolution()
    {
        using var ws = new TempWorkspace();
        var decompileDir = Path.Combine(ws.Root, "Lib", "DECOMPILE");
        Directory.CreateDirectory(decompileDir);

        var result = _sut.GetSolutionName(decompileDir);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("Lib", result.Value);
    }

    [Fact]
    public void GetSolutionName_sln_in_decompile_root_still_uses_solution_file_name()
    {
        using var ws = new TempWorkspace();
        var decompileDir = Path.Combine(ws.Root, "MyAssembly", "decompile");
        Directory.CreateDirectory(decompileDir);
        File.WriteAllText(Path.Combine(decompileDir, "Restored.sln"), "");

        var result = _sut.GetSolutionName(decompileDir);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("Restored", result.Value);
    }

    [Fact]
    public void GetSolutionName_nonexistent_directory_fails()
    {
        var result = _sut.GetSolutionName(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "nope"));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void FindProjects_without_csproj_fails()
    {
        using var ws = new TempWorkspace();

        var result = _sut.FindProjects(ws.Root);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void FindProjects_finds_csproj_in_subdirectory()
    {
        using var ws = new TempWorkspace();
        var csproj = ws.WriteFile("src/Core/Core.csproj", "<Project></Project>");

        var result = _sut.FindProjects(ws.Root);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Value);
        var project = Assert.Single(result.Value);
        Assert.Equal("Core", project.ProjectName);
        Assert.Equal(Path.GetDirectoryName(csproj), project.RootDirectory);
    }
}
