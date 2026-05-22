using SourceToAI.CLI.App.Cli;
using SourceToAI.CLI.App.Exceptions;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.App;

public sealed class GacPathResolverTests
{
    [Fact]
    public void ResolveRoot_ConfiguredPath_ReturnsFullPath()
    {
        using var ws = new TempWorkspace();
        var assemblyRoot = Path.Combine(ws.Root, "assembly");
        Directory.CreateDirectory(Path.Combine(assemblyRoot, "GAC_MSIL"));

        var resolved = GacPathResolver.ResolveRoot(assemblyRoot);

        Assert.Equal(Path.GetFullPath(assemblyRoot), resolved);
    }

    [Fact]
    public void ResolveRoot_MissingConfiguredPath_Throws()
    {
        using var ws = new TempWorkspace();
        var missing = Path.Combine(ws.Root, "does-not-exist");

        var ex = Assert.Throws<SourceToAiValidationException>(() => GacPathResolver.ResolveRoot(missing));
        Assert.Contains("existiert nicht", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveFlavorRoots_OnlyExistingFolders_ReturnedInOrder()
    {
        using var ws = new TempWorkspace();
        var assemblyRoot = Path.Combine(ws.Root, "Win", "Microsoft.NET", "assembly");
        Directory.CreateDirectory(Path.Combine(assemblyRoot, "GAC_MSIL"));
        Directory.CreateDirectory(Path.Combine(assemblyRoot, "GAC_32"));

        var flavors = GacPathResolver.ResolveFlavorRoots(assemblyRoot);

        Assert.Equal(2, flavors.Count);
        Assert.Equal("MSIL", flavors[0].FlavorLabel);
        Assert.Equal("32", flavors[1].FlavorLabel);
        Assert.Equal(0, flavors[0].Rank);
        Assert.Equal(1, flavors[1].Rank);
    }
}
