using SourceToAI.CLI.Services.Export.AiFeed;

namespace SourceToAI.Tests.Export.AiFeed;

public class AiFeedModelsTests
{
    [Fact]
    public void AiFeedContentHash_known_utf8_string_matches_first_eight_md5_hex_chars()
    {
        // MD5("hello") = 5d41402abc4b2a76b9719d911017c592
        Assert.Equal("5D41402A", AiFeedContentHash.ComputeMd5HexPrefix8("hello"));
    }

    [Fact]
    public void AiFeedContentHash_empty_string_matches_md5_of_empty_utf8()
    {
        Assert.Equal("D41D8CD9", AiFeedContentHash.ComputeMd5HexPrefix8(string.Empty));
    }

    [Fact]
    public void AiFeedContentHash_empty_span_matches_md5_of_empty()
    {
        Assert.Equal("D41D8CD9", AiFeedContentHash.ComputeMd5HexPrefix8(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void AiFeedFrontmatter_GetFileCount_null_is_zero()
    {
        Assert.Equal(0, AiFeedFrontmatter.GetFileCount(null));
    }

    [Fact]
    public void AiFeedFrontmatter_GetFileCount_empty_list_is_zero()
    {
        Assert.Equal(0, AiFeedFrontmatter.GetFileCount(Array.Empty<AiFeedManifestLine>()));
    }

    [Fact]
    public void AiFeedFrontmatter_GetFileCount_three_entries_is_three()
    {
        var list = Enumerable.Range(0, 3)
            .Select(i => new AiFeedManifestLine(i + 1, AiFeedManifestEntryType.Code, "00000000", 0, "x"))
            .ToList();

        Assert.Equal(3, AiFeedFrontmatter.GetFileCount(list));
    }

    [Fact]
    public void AiFeedFrontmatter_Create_sets_file_count_from_manifest()
    {
        var lines = new[]
        {
            new AiFeedManifestLine(1, AiFeedManifestEntryType.Code, "AAAAAAAA", 1, "a.cs"),
            new AiFeedManifestLine(2, AiFeedManifestEntryType.Doc, "BBBBBBBB", 2, "b.md"),
            new AiFeedManifestLine(3, AiFeedManifestEntryType.Code, "CCCCCCCC", 3, "c.cs"),
        };

        var session = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var generated = DateTimeOffset.Parse("2026-05-12T10:00:00Z");
        var fm = AiFeedFrontmatter.Create("MySolution", "MyProject", session, generated, lines);

        Assert.Equal(3, fm.FileCount);
        Assert.Equal("MySolution (MyProject)", fm.Project);
        Assert.Equal(AiFeedFrontmatter.DefaultFeedType, fm.FeedType);
        Assert.Equal(session, fm.SessionId);
        Assert.Equal(generated, fm.Generated);
    }

    [Fact]
    public void AiFeedManifestPath_uses_backslashes_in_output()
    {
        Assert.Equal(@"src\Sub\File.cs", AiFeedManifestPath.NormalizeForManifestTable("src/Sub/File.cs"));
    }

    [Fact]
    public void AiFeedExportedUtf8Size_counts_utf8_not_char_length()
    {
        // „€“ = 3 Bytes in UTF-8
        Assert.Equal(3, AiFeedExportedUtf8Size.OfExportedString("€"));
    }

    [Theory]
    [InlineData("Doc", AiFeedManifestEntryType.Doc)]
    [InlineData("Code", AiFeedManifestEntryType.Code)]
    [InlineData("UI", AiFeedManifestEntryType.Code)]
    public void AiFeedManifestEntryTypeMapping_from_file_type_category(string category, AiFeedManifestEntryType expected)
    {
        Assert.Equal(expected, AiFeedManifestEntryTypeMapping.FromFileTypeCategory(category));
    }
}
