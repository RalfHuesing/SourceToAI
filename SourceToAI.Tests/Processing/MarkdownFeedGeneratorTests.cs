using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.Processing;

public class MarkdownFeedGeneratorTests
{
    private static MarkdownFeedGenerator CreateSut()
    {
        return new MarkdownFeedGenerator(new CSharpDocumentLoader());
    }

    private readonly MarkdownFeedGenerator _sut = CreateSut();

    [Fact]
    public void GenerateFeed_includes_frontmatter_manifest_and_content()
    {
        using var ws = new TempWorkspace();
        var csPath = ws.WriteFile("src/Program.cs", "// entry");
        var project = new ProjectDefinition("MyApp", Path.Combine(ws.Root, "src", "MyApp.csproj"));

        var result = _sut.GenerateFeed("Sol", project, [csPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var md = result.Value!;

        Assert.Contains("feed_type: source_export", md);
        Assert.Contains("file_count: 1", md);
        Assert.Contains("project: \"Sol (MyApp)\"", md);
        Assert.Contains("| [1](#1) |", md);
        Assert.Contains("Program.cs", md);
        Assert.Contains("## CONTENT", md);
        Assert.Contains("````csharp", md); // mindestens 4 Backticks
        Assert.Contains("// entry", md);
    }

    [Fact]
    public void GenerateFeed_project_frontmatter_yaml_escapes_special_characters()
    {
        using var ws = new TempWorkspace();
        var csPath = ws.WriteFile("src/Program.cs", "// x");
        var project = new ProjectDefinition("P\nline2", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = _sut.GenerateFeed("S: \"x\"", project, [csPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var md = result.Value!;
        Assert.Contains("project: \"S: \\\"x\\\" (P\\nline2)\"", md);
    }

    [Fact]
    public void GenerateFeed_orders_markdown_before_other_files()
    {
        using var ws = new TempWorkspace();
        var csPath = ws.WriteFile("src/A.cs", "x");
        var mdPath = ws.WriteFile("src/B.md", "# doc");
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = _sut.GenerateFeed("S", project, [csPath, mdPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var md = result.Value!;
        var contentIdx = md.IndexOf("## CONTENT", StringComparison.Ordinal);
        Assert.True(contentIdx >= 0);
        var afterContent = md[contentIdx..];
        var firstFileIdx = afterContent.IndexOf("### [1]", StringComparison.Ordinal);
        var secondFileIdx = afterContent.IndexOf("### [2]", StringComparison.Ordinal);
        Assert.True(firstFileIdx >= 0 && secondFileIdx > firstFileIdx);
        Assert.Contains("B.md", afterContent[firstFileIdx..secondFileIdx]);
        Assert.Contains("A.cs", afterContent[secondFileIdx..]);
    }

    [Fact]
    public void GenerateFeed_uses_longer_fence_when_content_has_many_backticks()
    {
        using var ws = new TempWorkspace();
        // fünf aufeinanderfolgende Backticks im Inhalt → Fence-Länge 6 (plus Sprach-Tag-Zeile)
        var tricky = "prefix`````suffix";
        var path = ws.WriteFile("src/Tick.cs", tricky);
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = _sut.GenerateFeed("S", project, [path]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var md = result.Value!;
        Assert.Contains("``````csharp", md);
        Assert.Contains(tricky, md);
    }
}