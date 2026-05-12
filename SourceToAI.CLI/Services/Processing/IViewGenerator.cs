using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Processing;

/// <summary>
/// Wandelt den Syntaxbaum einer bereits geladenen C#-Datei in C#-Quelltext (eine View) um.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Kein Parser / kein File-I/O:</strong> Einlesen und <see cref="Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText"/>
/// erfolgen ausschließlich in <see cref="ICSharpDocumentLoader"/>; dieser Vertrag deckt nur Transformation bzw. spätere Rewrites ab.
/// </para>
/// <para>
/// <strong>Rückgabe wie <see cref="IFeedGenerator"/>:</strong> <see cref="ExtractionResult{T}"/> mit <c>string</c>.
/// Erfolg mit leerem <c>string</c> ist zulässig (z. B. leere Eingabe). Fehler nur bei echter Transformations- oder Validierungslogik.
/// </para>
/// <para>
/// <strong>Kein SemanticModel</strong> in der Basissignatur — nur Syntax; semantische Rewriter können später einen erweiterten Kontext erhalten.
/// </para>
/// </remarks>
public interface IViewGenerator
{
    /// <summary>Stabiler Schlüssel (Ordner-/Konzeptname), z. B. <c>complete</c>, <c>signatures-only</c>.</summary>
    string ViewKey { get; }

    ExtractionResult<string> Generate(CompilationUnitSyntax root, ViewGeneratorContext context);
}
