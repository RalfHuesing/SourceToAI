using SourceToAI.CLI.Services.Export;

namespace SourceToAI.Tests.Export;

public sealed class MultiViewExportPathsTests
{
    [Fact]
    public void GetSolutionExportRoot_combines_export_path_and_solution_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), "sta-solroot-" + Guid.NewGuid().ToString("N"));
        Assert.Equal(Path.Combine(root, "MySolution"), MultiViewExportPaths.GetSolutionExportRoot(root, "MySolution"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("...")]
    public void SanitizeFileNameSegment_empty_or_only_invalid_trim_yields_unnamed(string segment) =>
        Assert.Equal("unnamed", MultiViewExportPaths.SanitizeFileNameSegment(segment));

    [Fact]
    public void AllocateUniqueFileStem_null_used_set_throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            MultiViewExportPaths.AllocateUniqueFileStem("Any", null!));

    [Fact]
    public void GetViewOutputPath_three_arg_empty_stem_throws() =>
        Assert.Throws<ArgumentException>(() => MultiViewExportPaths.GetViewOutputPath(@"C:\out", "complete", ""));

    [Theory]
    [InlineData("complete", "complete")]
    [InlineData("signatures-only", "signatures-only")]
    [InlineData("public-only", "public-only")]
    [InlineData("dto-only", "dto-only")]
    public void GetViewFolderNameForViewKey_maps_known_keys(string viewKey, string expectedFolder)
    {
        Assert.Equal(expectedFolder, MultiViewExportPaths.GetViewFolderNameForViewKey(viewKey));
    }

    [Fact]
    public void GetViewFolderNameForViewKey_unknown_throws()
    {
        Assert.Throws<ArgumentException>(() => MultiViewExportPaths.GetViewFolderNameForViewKey("unknown-view"));
    }

    [Fact]
    public void SanitizeFileNameSegment_replaces_invalid_chars_and_trims()
    {
        Assert.Equal("a_b_c", MultiViewExportPaths.SanitizeFileNameSegment("a:b*c"));
        Assert.Equal("x", MultiViewExportPaths.SanitizeFileNameSegment("  x  "));
        Assert.Equal("y", MultiViewExportPaths.SanitizeFileNameSegment("y..."));
    }

    [Fact]
    public void SanitizeFileNameSegment_preserves_unicode_letters()
    {
        Assert.Equal("Projekt_Straße", MultiViewExportPaths.SanitizeFileNameSegment("Projekt:Straße"));
        Assert.Equal("한글", MultiViewExportPaths.SanitizeFileNameSegment("한글"));
    }

    [Fact]
    public void GetViewOutputPath_combines_root_folder_and_md_extension()
    {
        var root = Path.Combine(Path.GetTempPath(), "sta-export-test-" + Guid.NewGuid().ToString("N"));
        var path = MultiViewExportPaths.GetViewOutputPath(root, "complete", "MySol.MyApp");
        Assert.Equal(Path.Combine(root, "complete", "MySol.MyApp.md"), path);
    }

    [Fact]
    public void GetViewOutputPath_four_arg_overload_matches_stem_builder()
    {
        var root = @"C:\out\Sol";
        var expected = MultiViewExportPaths.GetViewOutputPath(
            root,
            "public-only",
            MultiViewExportPaths.BuildSanitizedExportFileStem("S<>", "P|"));
        var actual = MultiViewExportPaths.GetViewOutputPath(root, "public-only", "S<>", "P|");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Two_projects_whose_names_collide_after_sanitization_get_distinct_stems()
    {
        const string sol = "FixtureSol";
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var stem1 = MultiViewExportPaths.AllocateUniqueFileStem(
            MultiViewExportPaths.BuildSanitizedExportFileStem(sol, "a/b"),
            used);
        var stem2 = MultiViewExportPaths.AllocateUniqueFileStem(
            MultiViewExportPaths.BuildSanitizedExportFileStem(sol, "a:b"),
            used);

        Assert.Equal("FixtureSol.a_b", stem1);
        Assert.Equal("FixtureSol.a_b_2", stem2);
        Assert.Equal(2, used.Count);
    }

    [Fact]
    public void AllocateUniqueFileStem_reuses_suffix_counter_until_free()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "X.a_b", "X.a_b_2" };
        var stem = MultiViewExportPaths.AllocateUniqueFileStem("X.a_b", used);
        Assert.Equal("X.a_b_3", stem);
    }
}
