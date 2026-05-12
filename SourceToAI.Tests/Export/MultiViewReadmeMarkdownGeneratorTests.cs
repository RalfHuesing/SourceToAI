using SourceToAI.CLI.Services.Export;

namespace SourceToAI.Tests.Export;

public sealed class MultiViewReadmeMarkdownGeneratorTests
{
    [Fact]
    public void Generate_contains_manifest_content_views_and_dependency_graph_hint()
    {
        var sut = new MultiViewReadmeMarkdownGenerator();
        var text = sut.Generate("MyRepo", new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero));

        Assert.Contains("MANIFEST", text, StringComparison.Ordinal);
        Assert.Contains("CONTENT", text, StringComparison.Ordinal);
        Assert.Contains("pro Projekt", text, StringComparison.Ordinal);
        Assert.Contains("complete/", text, StringComparison.Ordinal);
        Assert.Contains("signatures-only/", text, StringComparison.Ordinal);
        Assert.Contains("public-only/", text, StringComparison.Ordinal);
        Assert.Contains("dto-only/", text, StringComparison.Ordinal);
        Assert.Contains("dependency-graph.md", text, StringComparison.Ordinal);
        Assert.Contains("Solution-Ebene", text, StringComparison.Ordinal);
        Assert.DoesNotContain("full-source.md", text, StringComparison.OrdinalIgnoreCase);
    }
}
