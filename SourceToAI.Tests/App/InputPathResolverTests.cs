using SourceToAI.CLI.App.Cli;
using SourceToAI.CLI.App.Exceptions;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.App;

public sealed class InputPathResolverTests
{
    [Fact]
    public void Resolve_WithoutWildcards_ReturnsOriginalPaths()
    {
        var input = new[] { @"C:\Repo\My.sln", @"D:\data\app.dll" };

        var result = InputPathResolver.Resolve(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void Resolve_WithFileWildcard_ExpandsToMatchingFiles()
    {
        using var ws = new TempWorkspace();
        ws.WriteFile("libA.dll", "");
        ws.WriteFile("libB.dll", "");
        ws.WriteFile("ignore.txt", "");

        var pattern = Path.Combine(ws.Root, "lib*.dll");
        var result = InputPathResolver.Resolve([pattern]);

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(File.Exists(p), p));
        Assert.Contains(Path.Combine(ws.Root, "libA.dll"), result, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(Path.Combine(ws.Root, "libB.dll"), result, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_WithWildcardNoMatches_ThrowsValidationException()
    {
        using var ws = new TempWorkspace();
        var pattern = Path.Combine(ws.Root, "nothing*.dll");

        var ex = Assert.Throws<SourceToAiValidationException>(() => InputPathResolver.Resolve([pattern]));

        Assert.Contains("Keine Treffer", ex.Message, StringComparison.Ordinal);
        Assert.Contains(pattern, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_WildcardMissingBaseDirectory_ThrowsValidationException()
    {
        using var ws = new TempWorkspace();
        var missingDir = Path.Combine(ws.Root, "does-not-exist");
        var pattern = Path.Combine(missingDir, "*.dll");

        var ex = Assert.Throws<SourceToAiValidationException>(() => InputPathResolver.Resolve([pattern]));

        Assert.Contains("Basisverzeichnis existiert nicht", ex.Message, StringComparison.Ordinal);
    }
}
