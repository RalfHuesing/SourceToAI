using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.Infrastructure;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Export.AiFeed;
using SourceToAI.CLI.Services.IO;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.CLI.Services.Processing.Markdown;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.Processing;

public class MarkdownProjectViewBuilderTests
{
    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileReader, PhysicalFileReader>();
        services.AddTransient<ICSharpDocumentLoader, CSharpDocumentLoader>();
        services.AddTransient<IFileTypeService, FileTypeService>();
        services.AddViewGenerators();
        services.AddMarkdownProjectViewBuilders();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void All_builders_expose_expected_relative_output_paths()
    {
        using var sp = CreateServiceProvider();
        var builders = sp.GetServices<IMarkdownProjectViewBuilder>().ToList();

        Assert.Equal(4, builders.Count);
        Assert.Contains(builders, b => b.RelativeOutputFile == "complete/full-source.md");
        Assert.Contains(builders, b => b.RelativeOutputFile == "signatures-only/signatures.md");
        Assert.Contains(builders, b => b.RelativeOutputFile == "public-only/public-api.md");
        Assert.Contains(builders, b => b.RelativeOutputFile == "dto-only/models.md");
    }

    [Fact]
    public void Complete_build_orders_markdown_before_cs_and_includes_both_as_segments()
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
        Assert.EndsWith("B.md", segs[0].RelativePathFromProjectRoot, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Doc", segs[0].FileTypeCategory);
        Assert.Equal("markdown", segs[0].FenceLanguage);
        Assert.Contains("# doc", segs[0].TransformedText, StringComparison.Ordinal);

        Assert.EndsWith("A.cs", segs[1].RelativePathFromProjectRoot, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Code", segs[1].FileTypeCategory);
        Assert.Equal("csharp", segs[1].FenceLanguage);
        Assert.Contains("// code", segs[1].TransformedText, StringComparison.Ordinal);
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
}
