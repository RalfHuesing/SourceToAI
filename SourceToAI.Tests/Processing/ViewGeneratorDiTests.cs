using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.Infrastructure;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.CLI.Services.Processing.ViewGenerators;

namespace SourceToAI.Tests.Processing;

public class ViewGeneratorDiTests
{
    [Fact]
    public void AddViewGenerators_resolves_four_distinct_implementations()
    {
        var services = new ServiceCollection();
        services.AddViewGenerators();
        using var sp = services.BuildServiceProvider();

        var generators = sp.GetServices<IViewGenerator>().ToList();

        Assert.Equal(4, generators.Count);
        Assert.Equal(4, generators.Select(g => g.ViewKey).Distinct().Count());
    }

    [Fact]
    public void View_generators_empty_compilation_unit_succeeds_with_whitespace_only_or_empty()
    {
        var root = SyntaxFactory.ParseCompilationUnit("");
        var context = new ViewGeneratorContext("empty.cs");

        IViewGenerator[] stubs =
        [
            new CompleteViewGenerator(),
            new SignaturesOnlyViewGenerator(),
            new PublicOnlyViewGenerator(),
            new DtoOnlyViewGenerator(),
        ];

        foreach (var g in stubs)
        {
            var result = g.Generate(root, context);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.True(string.IsNullOrWhiteSpace(result.Value), $"ViewKey={g.ViewKey}");
        }
    }

    [Fact]
    public void Complete_view_prefers_original_source_text_when_context_supplies_it()
    {
        var root = SyntaxFactory.ParseCompilationUnit("class X { }");
        var ctx = new ViewGeneratorContext("f.cs", "// preserved\n");
        var sut = new CompleteViewGenerator();
        var r = sut.Generate(root, ctx);
        Assert.True(r.IsSuccess);
        Assert.Equal("// preserved\n", r.Value);
    }
}
