using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Services.Processing.Rewriters;

namespace SourceToAI.Tests.Processing;

public class DtoRewriterTests
{
    private static readonly CSharpParseOptions ParseOptions =
        CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    [Fact]
    public void Rewrite_keeps_simple_dto_class_with_auto_property()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            public class OrderDto { public int Id { get; set; } }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = DtoRewriter.Rewrite(root).ToFullString();

        Assert.Contains("class OrderDto", output);
        Assert.Contains("public int Id { get; set; }", output);
        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    [Fact]
    public void Rewrite_removes_class_that_only_has_a_method()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            public class Service { public void Run() { } }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = DtoRewriter.Rewrite(root).ToFullString();

        Assert.DoesNotContain("Service", output);
        Assert.DoesNotContain("Run", output);
        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    [Fact]
    public void Rewrite_removes_entire_class_when_auto_property_and_expression_bodied_method()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            public class X { public int P { get; set; } public string M() => ""; }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = DtoRewriter.Rewrite(root).ToFullString();

        Assert.DoesNotContain("class X", output);
        Assert.DoesNotContain("public int P", output);
        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    [Fact]
    public void Rewrite_keeps_enum()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            public enum E { A, B }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = DtoRewriter.Rewrite(root).ToFullString();

        Assert.Contains("enum E", output);
        Assert.Contains("A", output);
        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    [Fact]
    public void Rewrite_keeps_positional_record()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            public record R(int A);
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = DtoRewriter.Rewrite(root).ToFullString();

        Assert.Contains("record R", output);
        Assert.Contains("int A", output);
        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    [Fact]
    public void Rewrite_keeps_dto_and_drops_service_class_in_same_unit()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            public class OrderDto { public int Id { get; set; } }
            public class BillingService { public void Charge() { } }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = DtoRewriter.Rewrite(root).ToFullString();

        Assert.Contains("OrderDto", output);
        Assert.DoesNotContain("BillingService", output);
        Assert.DoesNotContain("Charge", output);
        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    [Fact]
    public void Rewrite_keeps_outer_dto_when_nested_type_is_invalid()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            public class Outer
            {
                public int Id { get; set; }
                public class Inner { public void M() { } }
            }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = DtoRewriter.Rewrite(root).ToFullString();

        Assert.Contains("class Outer", output);
        Assert.Contains("public int Id { get; set; }", output);
        Assert.DoesNotContain("Inner", output);
        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    private static void AssertNoParseErrors(SyntaxTree tree, CancellationToken cancellationToken, string label)
    {
        var errors = tree.GetDiagnostics(cancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.True(errors.Count == 0, $"{label}: {string.Join("; ", errors)}");
    }
}
