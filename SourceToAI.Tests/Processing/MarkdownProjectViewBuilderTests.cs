using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.Infrastructure;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Export.AiFeed;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.CLI.Services.Processing.Markdown;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.Processing;

// @covers MarkdownProjectViewBuilderBase
public class MarkdownProjectViewBuilderTests
{
    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICSharpDocumentLoader, CSharpDocumentLoader>();
        services.AddViewGenerators();
        services.AddMarkdownProjectViewBuilders();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void All_builders_register_distinct_view_keys()
    {
        using var sp = CreateServiceProvider();
        var builders = sp.GetServices<IMarkdownProjectViewBuilder>().ToList();

        var loaderA = sp.GetRequiredService<ICSharpDocumentLoader>();
        var loaderB = sp.GetRequiredService<ICSharpDocumentLoader>();
        Assert.Same(loaderA, loaderB);

        Assert.Equal(4, builders.Count);
        var keys = builders.Select(b => b.ViewKey).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("complete", keys);
        Assert.Contains("signatures-only", keys);
        Assert.Contains("public-only", keys);
        Assert.Contains("dto-only", keys);
    }

    [Fact]
    public void Complete_build_orders_segments_by_path_and_includes_md_and_cs()
    {
        using var sp = CreateServiceProvider();
        var sut = sp.GetServices<IMarkdownProjectViewBuilder>().Single(b => b.ViewKey == "complete");

        using var ws = new TempWorkspace();
        var csPath = ws.WriteFile("src/A.cs", "// code");
        var mdPath = ws.WriteFile("src/B.md", "# doc");
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = sut.BuildContentSegments(project, [csPath, mdPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var segs = result.Value!;
        Assert.Equal(2, segs.Count);
        Assert.EndsWith("A.cs", segs[0].RelativePathFromProjectRoot, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Code", segs[0].FileTypeCategory);
        Assert.Equal("csharp", segs[0].FenceLanguage);
        Assert.Contains("// code", segs[0].TransformedText, StringComparison.Ordinal);

        Assert.EndsWith("B.md", segs[1].RelativePathFromProjectRoot, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Doc", segs[1].FileTypeCategory);
        Assert.Equal("markdown", segs[1].FenceLanguage);
        Assert.Contains("# doc", segs[1].TransformedText, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("complete")]
    [InlineData("signatures-only")]
    [InlineData("public-only")]
    public void Each_view_single_cs_file_yields_one_code_segment(string viewKey)
    {
        using var sp = CreateServiceProvider();
        var sut = sp.GetServices<IMarkdownProjectViewBuilder>().Single(b => b.ViewKey == viewKey);

        using var ws = new TempWorkspace();
        var csPath = ws.WriteFile("src/X.cs", "public class X { }");
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = sut.BuildContentSegments(project, [csPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var segs = result.Value!;
        Assert.Single(segs);
        Assert.EndsWith("X.cs", segs[0].RelativePathFromProjectRoot, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Code", segs[0].FileTypeCategory);
        Assert.Equal("csharp", segs[0].FenceLanguage);
        Assert.Contains("public class X", segs[0].TransformedText, StringComparison.Ordinal);
    }

    [Fact]
    public void Dto_only_single_file_yields_one_segment_when_dto_shapes_exist()
    {
        using var sp = CreateServiceProvider();
        var sut = sp.GetServices<IMarkdownProjectViewBuilder>().Single(b => b.ViewKey == "dto-only");

        const string source = """
            namespace N;

            public record OrderDto(System.Guid Id, string Label);
            """;

        using var ws = new TempWorkspace();
        var csPath = ws.WriteFile("src/Models.cs", source);
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = sut.BuildContentSegments(project, [csPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var segs = result.Value!;
        Assert.Single(segs);
        Assert.EndsWith("Models.cs", segs[0].RelativePathFromProjectRoot, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OrderDto", segs[0].TransformedText, StringComparison.Ordinal);
    }

    [Fact]
    public void Public_only_view_segment_contains_public_method_name_but_not_private_method_name()
    {
        using var sp = CreateServiceProvider();
        var sut = sp.GetServices<IMarkdownProjectViewBuilder>().Single(b => b.ViewKey == "public-only");

        const string source = """
            namespace N;

            public class Api
            {
                public void VisibleToExportTests() { }

                private void UniquePrivateMarkerForPublicViewTest() { }
            }
            """;

        using var ws = new TempWorkspace();
        var csPath = ws.WriteFile("src/Api.cs", source);
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = sut.BuildContentSegments(project, [csPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var text = result.Value![0].TransformedText;
        Assert.Contains("VisibleToExportTests", text, StringComparison.Ordinal);
        Assert.DoesNotContain("UniquePrivateMarkerForPublicViewTest", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Public_only_internal_only_file_does_not_yield_segment_when_only_public_api_shell_remains()
    {
        using var sp = CreateServiceProvider();
        var sut = sp.GetServices<IMarkdownProjectViewBuilder>().Single(b => b.ViewKey == "public-only");

        const string internalOnly = """
            namespace N;

            internal static class NotForPublicView { }
            """;

        using var ws = new TempWorkspace();
        var internalPath = ws.WriteFile("src/InternalOnly.cs", internalOnly);
        var publicPath = ws.WriteFile("src/PublicApi.cs", "public class Visible { public void M() { } }");
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = sut.BuildContentSegments(project, [internalPath, publicPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var segs = result.Value!;
        Assert.Single(segs);
        Assert.EndsWith("PublicApi.cs", segs[0].RelativePathFromProjectRoot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Non_cs_files_are_omitted_from_signatures_only_segments()
    {
        using var sp = CreateServiceProvider();
        var sut = sp.GetServices<IMarkdownProjectViewBuilder>().Single(b => b.ViewKey == "signatures-only");

        using var ws = new TempWorkspace();
        ws.WriteFile("src/note.md", "# only md");
        var csPath = ws.WriteFile("src/Z.cs", "public class Z { }");
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = sut.BuildContentSegments(project, [csPath, Path.Combine(ws.Root, "src/note.md")]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var segs = result.Value!;
        Assert.Single(segs);
        Assert.EndsWith("Z.cs", segs[0].RelativePathFromProjectRoot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("note.md", segs[0].RelativePathFromProjectRoot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Complete_segment_preserves_tricky_backtick_runs_for_composer_fencing()
    {
        using var sp = CreateServiceProvider();
        var sut = sp.GetServices<IMarkdownProjectViewBuilder>().Single(b => b.ViewKey == "complete");

        const string tricky = "prefix`````suffix";
        using var ws = new TempWorkspace();
        var csPath = ws.WriteFile("src/Tick.cs", tricky);
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = sut.BuildContentSegments(project, [csPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(tricky, result.Value![0].TransformedText);
    }

    [Fact]
    public void Complete_build_merges_parse_skip_warnings_when_cs_file_is_locked()
    {
        using var sp = CreateServiceProvider();
        var sut = sp.GetServices<IMarkdownProjectViewBuilder>().Single(b => b.ViewKey == "complete");

        using var ws = new TempWorkspace();
        var lockedCs = ws.WriteFile("src/Locked.cs", "class Locked { }");
        var okCs = ws.WriteFile("src/Ok.cs", "class Ok { }");
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        using (var hold = new FileStream(lockedCs, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            _ = hold;
            var result = sut.BuildContentSegments(project, [lockedCs, okCs]);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.NotNull(result.Warnings);
            Assert.Contains(result.Warnings!, w => w.Contains("Locked.cs", StringComparison.OrdinalIgnoreCase));
        Assert.Single(result.Value!);
        Assert.EndsWith("Ok.cs", result.Value![0].RelativePathFromProjectRoot, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Complete_build_includes_razor_and_html_under_wwwroot_as_text_segments()
    {
        using var sp = CreateServiceProvider();
        var sut = sp.GetServices<IMarkdownProjectViewBuilder>().Single(b => b.ViewKey == "complete");

        using var ws = new TempWorkspace();
        var htmlPath = ws.WriteFile("src/wwwroot/index.html", "<p>hi</p>");
        var razorPath = ws.WriteFile("src/Pages/Counter.razor", "@code { }");
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = sut.BuildContentSegments(project, [htmlPath, razorPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var segs = result.Value!;
        Assert.Equal(2, segs.Count);
        var htmlSeg = segs.Single(s => s.FenceLanguage.Equals("html", StringComparison.OrdinalIgnoreCase));
        var razorSeg = segs.Single(s => s.FenceLanguage.Equals("razor", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("wwwroot", htmlSeg.RelativePathFromProjectRoot, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("index.html", htmlSeg.RelativePathFromProjectRoot, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("UI", htmlSeg.FileTypeCategory);
        Assert.Contains("<p>hi</p>", htmlSeg.TransformedText, StringComparison.Ordinal);

        Assert.EndsWith("Counter.razor", razorSeg.RelativePathFromProjectRoot, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("UI", razorSeg.FileTypeCategory);
        Assert.Contains("@code", razorSeg.TransformedText, StringComparison.Ordinal);
    }
}
