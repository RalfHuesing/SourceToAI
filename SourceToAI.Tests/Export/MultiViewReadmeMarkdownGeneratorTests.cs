using SourceToAI.CLI.Services.Export;

namespace SourceToAI.Tests.Export;

public sealed class MultiViewReadmeMarkdownGeneratorTests
{
    [Fact]
    public void Generate_contains_manifest_content_views_and_dependency_graph_hint()
    {
        var sut = new MultiViewReadmeMarkdownGenerator();
        var text = sut.Generate("MyRepo", new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero));

        Assert.Contains("# SourceToAI — Globaler Export: MyRepo", text, StringComparison.Ordinal);
        Assert.Contains("`MyRepo`", text, StringComparison.Ordinal);
        Assert.Contains("`2026-05-12T10:00:00.000Z`", text, StringComparison.Ordinal);
        Assert.Contains("MANIFEST", text, StringComparison.Ordinal);
        Assert.Contains("CONTENT", text, StringComparison.Ordinal);
        Assert.Contains("pro Projekt", text, StringComparison.Ordinal);
        Assert.Contains("complete/", text, StringComparison.Ordinal);
        Assert.Contains("signatures-only/", text, StringComparison.Ordinal);
        Assert.Contains("public-only/", text, StringComparison.Ordinal);
        Assert.Contains("dto-only/", text, StringComparison.Ordinal);
        Assert.Contains("dependency-graph.md", text, StringComparison.Ordinal);
        Assert.Contains("**Solution-Ebene** (isoliert)", text, StringComparison.Ordinal);
        Assert.Contains("Isolated/", text, StringComparison.Ordinal);
        Assert.Contains("Merged/", text, StringComparison.Ordinal);
        Assert.Contains("-<view>.md", text, StringComparison.Ordinal);
        Assert.DoesNotContain("full-source.md", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_includes_repository_folder_name_in_unescaped_title_line()
    {
        var sut = new MultiViewReadmeMarkdownGenerator();
        const string folder = "My-Solution_2026";
        var text = sut.Generate(folder, DateTimeOffset.Parse("2026-01-02T03:04:05.000Z"));

        Assert.Contains($"# SourceToAI — Globaler Export: {folder}", text, StringComparison.Ordinal);
    }
}
