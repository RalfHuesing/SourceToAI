using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Services.Processing.Rewriters;

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
        var tree = CSharpSyntaxTree.ParseText(source, ParseOptions);
        AssertNoParseErrors(tree, "Eingabe");

        var root = tree.GetCompilationUnitRoot();
        var rewritten = SignaturesRewriter.Rewrite(root);
        var output = rewritten.ToFullString();

        foreach (var fragment in forbiddenFragments)
            Assert.DoesNotContain(fragment, output);

        var roundTrip = CSharpSyntaxTree.ParseText(output, ParseOptions);
        var errors = roundTrip.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void Rewrite_top_level_local_function_strips_expression_body()
    {
        const string source = """
            static int F() => 3;
            """;

        var tree = CSharpSyntaxTree.ParseText(source, ParseOptions);
        var root = tree.GetCompilationUnitRoot();
        var output = SignaturesRewriter.Rewrite(root).ToFullString();

        Assert.DoesNotContain("=>", output);
        var roundTrip = CSharpSyntaxTree.ParseText(output, ParseOptions);
        AssertNoParseErrors(roundTrip, "Ausgabe");
    }

    [Fact]
    public void Rewrite_property_expression_body_yields_accessor_list()
    {
        const string source = """
            public class C { public int P => 1; }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions).GetCompilationUnitRoot();
        var output = SignaturesRewriter.Rewrite(root).ToFullString();

        Assert.Contains("get;", output);
        Assert.DoesNotContain("=>", output);
    }

    private static void AssertNoParseErrors(SyntaxTree tree, string label)
    {
        var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, $"{label}: {string.Join("; ", errors)}");
    }
}
