using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceToAI.CLI.Models;

/// <summary>
/// Eine <c>.cs</c>-Datei nach genau einem Dateizugriff plus Roslyn-Parse.
/// Dient als gemeinsame Eingabe für mehrere View-Builder (Parse Once, Rewrite Multiple).
/// </summary>
/// <remarks>
/// Ungültiges C#: Roslyn erzeugt trotzdem einen <see cref="SyntaxTree"/>; Parserfehler
/// liegen in <see cref="ParseErrorMessages"/>. Die Datei wird nicht verworfen — Downstream
/// kann weiterhin <see cref="SourceText"/> (Complete-View) oder den AST nutzen.
/// </remarks>
public sealed class ParsedCSharpDocument
{
    public ParsedCSharpDocument(
        string absolutePath,
        string relativePath,
        string sourceText,
        SyntaxTree syntaxTree,
        long sizeBytes,
        IReadOnlyList<string> parseErrorMessages)
    {
        AbsolutePath = absolutePath;
        RelativePath = relativePath;
        SourceText = sourceText;
        SyntaxTree = syntaxTree;
        SizeBytes = sizeBytes;
        ParseErrorMessages = parseErrorMessages;
    }

    public string AbsolutePath { get; }

    public string RelativePath { get; }

    /// <summary>Originaltext für die Complete-View ohne erneutes Einlesen.</summary>
    public string SourceText { get; }

    public SyntaxTree SyntaxTree { get; }

    public CompilationUnitSyntax Root => SyntaxTree.GetCompilationUnitRoot();

    public long SizeBytes { get; }

    public IReadOnlyList<string> ParseErrorMessages { get; }

    public bool HasParseErrors => ParseErrorMessages.Count > 0;
}
