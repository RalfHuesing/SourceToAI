using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.Infrastructure;
using SourceToAI.CLI.Models;
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
    public void Complete_build_orders_markdown_before_cs_and_includes_both()
    {
        using var sp = CreateServiceProvider();
        var sut = sp.GetServices<IMarkdownProjectViewBuilder>().Single(b => b.ViewKey == "complete");

        using var ws = new TempWorkspace();
        var csPath = ws.WriteFile("src/A.cs", "// code");
        var mdPath = ws.WriteFile("src/B.md", "# doc");
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = sut.BuildMarkdown(project, [csPath, mdPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var md = result.Value!;
        var bMd = md.IndexOf("### B.md", StringComparison.Ordinal);
        var aCs = md.IndexOf("### A.cs", StringComparison.Ordinal);
        Assert.True(bMd >= 0 && aCs > bMd);
        Assert.Contains("````markdown", md);
        Assert.Contains("````csharp", md);
        Assert.Contains("# doc", md);
        Assert.Contains("// code", md);
    }

    [Theory]
    [InlineData("complete")]
    [InlineData("signatures-only")]
    [InlineData("public-only")]
    [InlineData("dto-only")]
    public void Each_view_builder_emits_path_header_and_csharp_fence_for_cs(string viewKey)
    {
        using var sp = CreateServiceProvider();
        var sut = sp.GetServices<IMarkdownProjectViewBuilder>().Single(b => b.ViewKey == viewKey);

        using var ws = new TempWorkspace();
        var csPath = ws.WriteFile("src/X.cs", "public class X { }");
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = sut.BuildMarkdown(project, [csPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var md = result.Value!;
        Assert.Contains("### X.cs", md);
        Assert.Contains("````csharp", md);
    }

    [Fact]
    public void Public_only_view_contains_public_method_name_but_not_private_method_name()
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

        var result = sut.BuildMarkdown(project, [csPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var md = result.Value!;
        Assert.Contains("VisibleToExportTests", md);
        Assert.DoesNotContain("UniquePrivateMarkerForPublicViewTest", md);
    }

    [Fact]
    public void Non_cs_files_are_omitted_from_signatures_only_output()
    {
        using var sp = CreateServiceProvider();
        var sut = sp.GetServices<IMarkdownProjectViewBuilder>().Single(b => b.ViewKey == "signatures-only");

        using var ws = new TempWorkspace();
        ws.WriteFile("src/note.md", "# only md");
        var csPath = ws.WriteFile("src/Z.cs", "public class Z { }");
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = sut.BuildMarkdown(project, [csPath, Path.Combine(ws.Root, "src/note.md")]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.DoesNotContain("note.md", result.Value!);
        Assert.Contains("### Z.cs", result.Value!);
    }

    [Fact]
    public void Builder_uses_longer_fence_when_cs_content_has_many_backticks()
    {
        using var sp = CreateServiceProvider();
        var sut = sp.GetServices<IMarkdownProjectViewBuilder>().Single(b => b.ViewKey == "complete");

        const string tricky = "prefix`````suffix";
        using var ws = new TempWorkspace();
        var csPath = ws.WriteFile("src/Tick.cs", tricky);
        var project = new ProjectDefinition("P", Path.Combine(ws.Root, "src", "P.csproj"));

        var result = sut.BuildMarkdown(project, [csPath]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Contains("``````csharp", result.Value!);
        Assert.Contains("### Tick.cs", result.Value!);
        Assert.Contains(tricky, result.Value!);
    }
}
