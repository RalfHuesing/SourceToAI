using SourceToAI.CLI.Services.Export.AiFeed;

namespace SourceToAI.Tests.Export.AiFeed;

public class AiFeedSegmentExportabilityTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("\r\n\t", false)]
    public void IsExportable_non_csharp_whitespace_only_never_exportable(string text, bool expected) =>
        Assert.Equal(
            expected,
            AiFeedSegmentExportability.IsExportable(
                new AiFeedContentSegment("x.md", "Doc", "markdown", text),
                AiFeedTransformedContentKind.OriginalAsTransformed));

    [Fact]
    public void IsExportable_rewritten_csharp_whitespace_shell_namespace_only_is_false()
    {
        var seg = new AiFeedContentSegment(
            "InternalOnly.cs",
            "Code",
            "csharp",
            """
            namespace N;

            """,
            CSharpRewrittenHasExportableSurface: false);
        Assert.False(AiFeedSegmentExportability.IsExportable(seg, AiFeedTransformedContentKind.RewrittenViewOutput));
    }

    [Fact]
    public void IsExportable_original_csharp_namespace_shell_still_exportable()
    {
        var seg = new AiFeedContentSegment("InternalOnly.cs", "Code", "csharp", """
            namespace N;

            """);
        Assert.True(AiFeedSegmentExportability.IsExportable(seg, AiFeedTransformedContentKind.OriginalAsTransformed));
    }

    [Fact]
    public void IsExportable_rewritten_csharp_public_type_is_true()
    {
        var seg = new AiFeedContentSegment(
            "A.cs",
            "Code",
            "csharp",
            "public class A { }",
            CSharpRewrittenHasExportableSurface: true);
        Assert.True(AiFeedSegmentExportability.IsExportable(seg, AiFeedTransformedContentKind.RewrittenViewOutput));
    }

    [Fact]
    public void IsExportable_rewritten_csharp_missing_surface_flag_throws()
    {
        var seg = new AiFeedContentSegment("x.cs", "Code", "csharp", "public class X { }");
        Assert.Throws<InvalidOperationException>(() =>
            AiFeedSegmentExportability.IsExportable(seg, AiFeedTransformedContentKind.RewrittenViewOutput));
    }

    [Fact]
    public void FilterToExportableList_rewritten_preserves_order_and_drops_empty()
    {
        var segments = new[]
        {
            new AiFeedContentSegment("a.cs", "Code", "csharp", "public class A { }", true),
            new AiFeedContentSegment("empty.cs", "Code", "csharp", "  \n  "),
            new AiFeedContentSegment("shell.cs", "Code", "csharp", "namespace X;\r\n", false),
            new AiFeedContentSegment("b.cs", "Code", "csharp", "public class B { }", true),
        };

        var filtered = AiFeedSegmentExportability.FilterToExportableList(
            segments,
            AiFeedTransformedContentKind.RewrittenViewOutput);

        Assert.Equal(2, filtered.Count);
        Assert.EndsWith("a.cs", filtered[0].RelativePathFromProjectRoot, StringComparison.Ordinal);
        Assert.EndsWith("b.cs", filtered[1].RelativePathFromProjectRoot, StringComparison.Ordinal);
    }
}
