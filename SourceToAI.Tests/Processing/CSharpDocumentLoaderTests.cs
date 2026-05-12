using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.Processing;

/// <summary>
/// Regression für <c>Parse Once, Rewrite Multiple</c> auf <see cref="ICSharpDocumentLoader"/>:
/// gemeinsamer Parse-Cache pro Prozess, <see cref="ICSharpDocumentLoader.Clear"/> zwischen Export-Läufen.
/// Nachweis-Pflicht: siehe <c>todo/refactor/06-tests-querschnitt-regression-und-benchmark-notizen.md</c>.
/// </summary>
public class CSharpDocumentLoaderTests
{
    [Fact]
    public void LoadParsedDocuments_returns_one_entry_per_unique_cs_file()
    {
        using var ws = new TempWorkspace();
        var p1 = ws.WriteFile("src/A.cs", "namespace N; class A { }");
        var p2 = ws.WriteFile("src/B.cs", "namespace N; class B { }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "src", "App.csproj"));

        var sut = new CSharpDocumentLoader();

        var result = sut.LoadParsedDocuments(project, [p1, p2]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public void LoadParsedDocuments_reuses_same_syntax_tree_for_two_notional_consumers()
    {
        using var ws = new TempWorkspace();
        var path = ws.WriteFile("src/C.cs", "class C { int X => 1; }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "src", "App.csproj"));
        var sut = new CSharpDocumentLoader();

        var doc = sut.LoadParsedDocuments(project, [path]).Value!.Single();

        CompilationUnitSyntax consumer1 = doc.Root;
        CompilationUnitSyntax consumer2 = doc.Root;

        Assert.Same(consumer1, consumer2);
        Assert.True(consumer1.Members.Any());
    }

    [Fact]
    public void LoadParsedDocuments_second_invocation_reuses_parse_cache_so_same_source_and_syntax_tree()
    {
        using var ws = new TempWorkspace();
        var path = ws.WriteFile("src/E.cs", "class E { }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "src", "App.csproj"));
        var sut = new CSharpDocumentLoader();

        var r1 = sut.LoadParsedDocuments(project, [path]);
        var r2 = sut.LoadParsedDocuments(project, [path]);

        Assert.True(r1.IsSuccess && r2.IsSuccess, r1.ErrorMessage ?? r2.ErrorMessage);
        Assert.Same(r1.Value![0].SourceText, r2.Value![0].SourceText);
        Assert.Same(r1.Value![0].SyntaxTree, r2.Value![0].SyntaxTree);
    }

    [Fact]
    public void Clear_discards_parse_cache_so_subsequent_load_reads_fresh_content_from_disk()
    {
        using var ws = new TempWorkspace();
        var path = ws.WriteFile("src/F.cs", "class F { const int V = 1; }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "src", "App.csproj"));
        var sut = new CSharpDocumentLoader();

        var r1 = sut.LoadParsedDocuments(project, [path]);
        Assert.True(r1.IsSuccess, r1.ErrorMessage);
        Assert.Contains("V = 1", r1.Value![0].SourceText, StringComparison.Ordinal);

        File.WriteAllText(path, "class F { const int V = 2; }");
        sut.Clear();
        var r2 = sut.LoadParsedDocuments(project, [path]);

        Assert.True(r2.IsSuccess, r2.ErrorMessage);
        Assert.Contains("V = 2", r2.Value![0].SourceText, StringComparison.Ordinal);
        Assert.DoesNotContain("V = 1", r2.Value![0].SourceText, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadParsedDocuments_deduplicates_identical_path()
    {
        using var ws = new TempWorkspace();
        var path = ws.WriteFile("src/D.cs", "class D { }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "src", "App.csproj"));
        var sut = new CSharpDocumentLoader();

        var result = sut.LoadParsedDocuments(project, [path, path]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Single(result.Value!);
    }

    /// <summary>
    /// Ungültiges C#: siehe XML-Dokumentation auf <see cref="ParsedCSharpDocument"/> —
    /// Dokument bleibt erhalten, <see cref="ParsedCSharpDocument.HasParseErrors"/> ist wahr.
    /// </summary>
    [Fact]
    public void LoadParsedDocuments_invalid_csharp_sets_HasParseErrors_but_keeps_tree_and_source()
    {
        using var ws = new TempWorkspace();
        var path = ws.WriteFile("src/Broken.cs", "class Broken { void M( }"); // Syntaxfehler
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "src", "App.csproj"));
        var sut = new CSharpDocumentLoader();

        var result = sut.LoadParsedDocuments(project, [path]);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var doc = result.Value!.Single();
        Assert.True(doc.HasParseErrors);
        Assert.NotEmpty(doc.ParseErrorMessages);
        Assert.Contains("class Broken", doc.SourceText, StringComparison.Ordinal);
        Assert.NotNull(doc.SyntaxTree);
        Assert.NotNull(doc.Root);
    }

    [Fact]
    public void LoadParsedDocuments_skips_exclusive_locked_cs_with_warning_and_loads_other_files()
    {
        using var ws = new TempWorkspace();
        var lockedPath = ws.WriteFile("src/Locked.cs", "class Locked { }");
        var okPath = ws.WriteFile("src/Ok.cs", "class Ok { }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "src", "App.csproj"));
        var sut = new CSharpDocumentLoader();

        using (var hold = new FileStream(lockedPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            _ = hold; // exklusiv — paralleles ReadAllText schlägt mit IOException fehl
            var result = sut.LoadParsedDocuments(project, [lockedPath, okPath]);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.NotNull(result.Warnings);
            Assert.Contains(result.Warnings!, w => w.Contains("Locked.cs", StringComparison.OrdinalIgnoreCase));
            Assert.Single(result.Value!);
            Assert.EndsWith("Ok.cs", result.Value![0].AbsolutePath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
