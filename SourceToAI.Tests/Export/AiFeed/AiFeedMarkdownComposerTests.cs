using System.Text.RegularExpressions;
using SourceToAI.CLI.Services.Export.AiFeed;

namespace SourceToAI.Tests.Export.AiFeed;

public class AiFeedMarkdownComposerTests
{
    private static readonly AiFeedMarkdownComposer Composer = new();

    private static readonly Guid FixedSession = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateTimeOffset FixedGenerated = DateTimeOffset.Parse("2026-05-12T12:34:56+00:00", null, System.Globalization.DateTimeStyles.RoundtripKind);

    [Fact]
    public void Compose_two_segments_frontmatter_manifest_and_content_counts_match()
    {
        var segments = new[]
        {
            new AiFeedContentSegment("src/A.cs", "Code", "csharp", "// alpha"),
            new AiFeedContentSegment("docs/B.md", "Doc", "markdown", "# Beta"),
        };

        var md = Composer.Compose("Sol", "Proj", FixedSession, FixedGenerated, segments);

        Assert.Contains("feed_type: source_export", md);
        Assert.Contains("project: \"Sol (Proj)\"", md);
        Assert.Contains($"session_id: {FixedSession:D}", md);
        Assert.Contains("generated: \"2026-05-12T12:34:56.0000000+00:00\"", md);
        Assert.Contains("file_count: 2", md);

        Assert.Contains("## MANIFEST", md);
        var manifestBodyLines = md.Split('\n').Where(l => l.StartsWith("| [", StringComparison.Ordinal)).ToArray();
        Assert.Equal(2, manifestBodyLines.Length);
        Assert.Contains("| [1](#1) | Code |", md);
        Assert.Contains("| [2](#2) | Doc |", md);

        Assert.Equal(2, Regex.Matches(md, @"^### \[\d+\] ", RegexOptions.Multiline).Count);
        Assert.Contains("### [1] src\\A.cs", md);
        Assert.Contains("### [2] docs\\B.md", md);
    }

    [Fact]
    public void Compose_content_with_long_backtick_run_uses_fence_at_least_four_backticks()
    {
        const string tricky = "text ````` trailing";
        var segments = new[]
        {
            new AiFeedContentSegment("x.cs", "Code", "csharp", tricky),
        };

        var md = Composer.Compose("S", "P", FixedSession, FixedGenerated, segments);

        var fenceOpen = Regex.Match(md, @"^(`{4,})csharp", RegexOptions.Multiline);
        Assert.True(fenceOpen.Success);
        Assert.True(fenceOpen.Groups[1].Length >= 4);

        var fenceClose = Regex.Match(md, @"^(`{4,})\s*$", RegexOptions.Multiline);
        Assert.True(fenceClose.Success);
        Assert.Equal(fenceOpen.Groups[1].Length, fenceClose.Groups[1].Length);
    }

    [Fact]
    public void Compose_zero_segments_valid_markdown_no_content_subsections()
    {
        var md = Composer.Compose("Sol", "Proj", FixedSession, FixedGenerated, Array.Empty<AiFeedContentSegment>());

        Assert.Contains("file_count: 0", md);
        Assert.Contains("## MANIFEST", md);
        Assert.Contains("|---:|:---|:---|---:|:---|", md);
        Assert.False(Regex.IsMatch(md, @"^\| \[\d+\]", RegexOptions.Multiline));
        Assert.Contains("## CONTENT", md);
        Assert.False(Regex.IsMatch(md, @"^### \[\d+\] ", RegexOptions.Multiline));
    }

    [Fact]
    public void Compose_project_field_with_special_characters_is_yaml_escaped_in_frontmatter()
    {
        var md = Composer.Compose("S: \"x\"", "P\nline2", FixedSession, FixedGenerated, Array.Empty<AiFeedContentSegment>());

        Assert.Contains("project: \"S: \\\"x\\\" (P\\nline2)\"", md);
    }

    [Fact]
    public void Compose_instruction_contains_project_display_name_quoted()
    {
        var md = Composer.Compose("Sol", "MyProj", FixedSession, FixedGenerated, Array.Empty<AiFeedContentSegment>());
        Assert.Contains("Dies ist Projekt: 'MyProj'.", md);
    }

    [Fact]
    public void Compose_hashes_match_AiFeedContentHash_for_segment_text()
    {
        const string body = "hello";
        var expectedHash = AiFeedContentHash.ComputeMd5HexPrefix8(body);
        var segments = new[] { new AiFeedContentSegment("f.cs", "Code", "csharp", body) };

        var md = Composer.Compose("S", "P", FixedSession, FixedGenerated, segments);

        Assert.Contains($"| [1](#1) | Code | {expectedHash} |", md);
    }
}
