using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Services.Processing.Rewriters;

namespace SourceToAI.Tests.Processing;

public class VisibilityRewriterTests
{
    private static readonly CSharpParseOptions ParseOptions =
        CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    [Fact]
    public void public_only_rewrite_excludes_private_methods_from_output()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            public class Sample
            {
                public void A() { }
                private void Secret() { }
                internal class X { }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct);
        var root = tree.GetCompilationUnitRoot(ct);
        var output = VisibilityRewriter.Rewrite(root).ToFullString();

        Assert.Contains("public void A()", output);
        Assert.DoesNotContain("Secret", output);
        Assert.DoesNotContain("class X", output);

        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    [Fact]
    public void Rewrite_keeps_public_and_protected_members()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            public class C
            {
                public void Pub() { }
                protected void Prot() { }
            }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = VisibilityRewriter.Rewrite(root).ToFullString();

        Assert.Contains("public void Pub()", output);
        Assert.Contains("protected void Prot()", output);
        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    [Fact]
    public void Rewrite_removes_top_level_internal_type_declaration()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            namespace N
            {
                internal class Hidden { public void M() { } }
                public class Visible { public void M() { } }
            }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = VisibilityRewriter.Rewrite(root).ToFullString();

        Assert.DoesNotContain("Hidden", output);
        Assert.Contains("public class Visible", output);
        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    [Fact]
    public void Rewrite_removes_implicit_internal_top_level_class_without_public_modifier()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            namespace N
            {
                class ImplicitInternal { }
            }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = VisibilityRewriter.Rewrite(root).ToFullString();

        Assert.DoesNotContain("ImplicitInternal", output);
        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    [Fact]
    public void Rewrite_nested_private_type_is_removed()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            public class Outer
            {
                private class PrivateChildType { }
                public class PublicChildType { }
            }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = VisibilityRewriter.Rewrite(root).ToFullString();

        Assert.DoesNotContain("PrivateChildType", output);
        Assert.Contains("public class PublicChildType", output);
        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    [Fact]
    public void Rewrite_top_level_local_function_removed_preserves_roundtrip_for_empty_cu()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            static int TopLevelLocal() => 3;
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = VisibilityRewriter.Rewrite(root).ToFullString();

        Assert.DoesNotContain("TopLevelLocal", output);
        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    [Fact]
    public void Rewrite_method_scoped_local_function_keeps_compilation()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            public class C
            {
                public int Host()
                {
                    int LocalFn() => 40;
                    return LocalFn() + 2;
                }
            }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = VisibilityRewriter.Rewrite(root).ToFullString();

        Assert.Contains("LocalFn", output);
        AssertNoParseErrors(CSharpSyntaxTree.ParseText(output, ParseOptions, cancellationToken: ct), ct, "Ausgabe");
    }

    [Fact]
    public void Rewrite_removes_member_with_protected_internal_accessibility()
    {
        var ct = TestContext.Current.CancellationToken;
        const string source = """
            public class C
            {
                protected internal void Pi() { }
                protected void Keep() { }
            }
            """;

        var root = CSharpSyntaxTree.ParseText(source, ParseOptions, cancellationToken: ct).GetCompilationUnitRoot(ct);
        var output = VisibilityRewriter.Rewrite(root).ToFullString();

        Assert.DoesNotContain("Pi()", output);
        Assert.Contains("protected void Keep()", output);
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
