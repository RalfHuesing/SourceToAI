using SourceToAI.CLI.Services.Export;

namespace SourceToAI.Tests.Export;

public sealed class MultiViewReadmeMarkdownGeneratorTests
{
    private static readonly DateTimeOffset FixedStamp = new(2026, 5, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GenerateGlobalExportOverview_contains_structure_rg_and_views()
    {
        var sut = new MultiViewReadmeMarkdownGenerator();
        var text = sut.GenerateGlobalExportOverview(FixedStamp);

        Assert.Contains("# SourceToAI — Export-Verzeichnis (KI-Orientierung)", text, StringComparison.Ordinal);
        Assert.Contains("`2026-05-12T10:00:00.000Z`", text, StringComparison.Ordinal);
        Assert.Contains("MANIFEST", text, StringComparison.Ordinal);
        Assert.Contains("CONTENT", text, StringComparison.Ordinal);
        Assert.Contains("Isolated", text, StringComparison.Ordinal);
        Assert.Contains("Merged", text, StringComparison.Ordinal);
        Assert.Contains("rg", text, StringComparison.Ordinal);
        Assert.Contains("complete", text, StringComparison.Ordinal);
        Assert.Contains("signatures-only", text, StringComparison.Ordinal);
        Assert.Contains("public-only", text, StringComparison.Ordinal);
        Assert.Contains("dto-only", text, StringComparison.Ordinal);
        Assert.Contains("Best Practice für KIs", text, StringComparison.Ordinal);
        Assert.Contains("Definitionen zuerst", text, StringComparison.Ordinal);
        Assert.DoesNotContain("full-source.md", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateIsolatedSolutionReadme_contains_manifest_content_views_and_dependency_graph_hint()
    {
        var sut = new MultiViewReadmeMarkdownGenerator();
        var text = sut.GenerateIsolatedSolutionReadme("MySol", "MyRepo", FixedStamp);

        Assert.Contains("# SourceToAI — Isolierter Export: MySol", text, StringComparison.Ordinal);
        Assert.Contains("`MySol`", text, StringComparison.Ordinal);
        Assert.Contains("`MyRepo`", text, StringComparison.Ordinal);
        Assert.Contains("`2026-05-12T10:00:00.000Z`", text, StringComparison.Ordinal);
        Assert.Contains("MANIFEST", text, StringComparison.Ordinal);
        Assert.Contains("CONTENT", text, StringComparison.Ordinal);
        Assert.Contains("pro Projekt", text, StringComparison.Ordinal);
        Assert.Contains("./complete/", text, StringComparison.Ordinal);
        Assert.Contains("../Merged/", text, StringComparison.Ordinal);
        Assert.Contains("dependency-graph.md", text, StringComparison.Ordinal);
        Assert.Contains("Solution-Ebene", text, StringComparison.Ordinal);
        Assert.Contains("Gezielte Suche (KI)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("full-source.md", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateIsolatedSolutionReadme_includes_repository_folder_name_in_title_meta()
    {
        var sut = new MultiViewReadmeMarkdownGenerator();
        const string folder = "My-Solution_2026";
        var text = sut.GenerateIsolatedSolutionReadme("SolX", folder, DateTimeOffset.Parse("2026-01-02T03:04:05.000Z"));

        Assert.Contains("# SourceToAI — Isolierter Export: SolX", text, StringComparison.Ordinal);
        Assert.Contains($"`{folder}`", text, StringComparison.Ordinal);
    }
}
