using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Services.Processing.Rewriters;
using System.Text;

namespace SourceToAI.Tests.Processing;

public class SignaturesRewriterTests
{
    private static readonly CSharpParseOptions ParseOptions =
        CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    public static TheoryData<string, string[]> SignaturesCases => new()
    {
        {
            """
            public class C
            {
                public int Blocked(int a) { return a + 1; }
            }
            """,
            ["return a + 1", "{ return"]
        },
        {
            """
            public class C
            {
                public int Arrow() => 42;
            }
            """,
            ["=>"]
        },
        {
            """
            public class C
            {
                public int PropArrow => 7;
            }
            """,
            ["=>"]
        },
        {
            """
            public class C
            {
                public int Auto { get; set; } = 99;
            }
            """,
            ["= 99"]
        },
        {
            """
            public class C
            {
                public C() : base() { int x = 1; }
                static C() { }
                ~C() { }
            }
            """,
            ["int x = 1"]
        },
        {
            """
            public class C
            {
                public static implicit operator int(C c) => 0;
                public static C operator +(C a, C b) { return a; }
            }
            """,
            ["=>", "return a"]
        },
        {
            """
            public class C
            {
                public int this[int i] { get { return i; } set { } }
            }
            """,
            ["return i"]
        },
        {
            """
            public class C
            {
                public event System.EventHandler E
                {
                    add { }
                    remove { }
                }
            }
            """,
            ["add {", "remove {"]
        },
        {
            """
            public record Point(int X, int Y);
            """,
            []
        },
        {
            """
            public sealed class Box(int value)
            {
                public int Read() => value;
            }
            """,
            ["=>"]
        },
    };

    [Theory]
    [MemberData(nameof(SignaturesCases))]
    public void Rewrite_output_parses_without_errors_and_strips_bodies(string source, string[] forbiddenFragments)
    {
        var ct = TestContext.Current.CancellationToken;
        var tree = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct);
        AssertNoParseErrors(tree, "Eingabe", ct);

        var root = tree.GetCompilationUnitRoot(ct);
        var rewritten = SignaturesRewriter.Rewrite(root);
        var output = rewritten.ToFullString();

        foreach (var fragment in forbiddenFragments)
            Assert.DoesNotContain(fragment, output);

        var roundTrip = CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct);
        var errors = roundTrip.GetDiagnostics(ct).Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void Rewrite_top_level_local_function_strips_expression_body()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            static int F() => 3;
            """;

        var tree = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct);
        var root = tree.GetCompilationUnitRoot(ct);
        var output = SignaturesRewriter.Rewrite(root).ToFullString();

        Assert.DoesNotContain("=>", output);
        var roundTrip = CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct);
        AssertNoParseErrors(roundTrip, "Ausgabe", ct);
    }

    [Fact]
    public void Rewrite_many_mixed_members_round_trips_without_errors()
    {
        var ct = TestContext.Current.CancellationToken;
        const int count = 400;
        var sb = new StringBuilder("""
            public static class Bulk
            {
            """);

        for (var i = 0; i < count; i++)
        {
            sb.Append("public static int E").Append(i).Append("() => ").Append(i).AppendLine(";");
            sb.Append("public static void B").Append(i).AppendLine("() { int _ = 1; }");
        }

        sb.AppendLine("}");
        var source = sb.ToString();

        var tree = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct);
        AssertNoParseErrors(tree, "Eingabe (Bulk)", ct);

        var root = tree.GetCompilationUnitRoot(ct);
        var rewritten = SignaturesRewriter.Rewrite(root);
        var output = rewritten.ToFullString();

        Assert.DoesNotContain("int _ = 1", output);
        Assert.DoesNotContain("=>", output);

        var roundTrip = CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct);
        var errors = roundTrip.GetDiagnostics(ct).Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void Rewrite_property_expression_body_yields_accessor_list()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            public class C { public int P => 1; }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = SignaturesRewriter.Rewrite(root).ToFullString();

        Assert.Contains("get;", output);
        Assert.DoesNotContain("=>", output);
    }

    private static void AssertNoParseErrors(SyntaxTree tree, string label, CancellationToken cancellationToken)
    {
        var errors = tree.GetDiagnostics(cancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, $"{label}: {string.Join("; ", errors)}");
    }
}
