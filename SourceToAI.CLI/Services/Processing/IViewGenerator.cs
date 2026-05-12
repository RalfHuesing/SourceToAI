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
/// <strong>Rückgabe:</strong> <see cref="ExtractionResult{T}"/> mit <see cref="ViewGenerationResult"/> (Ausgabetext und
/// <see cref="ViewGenerationResult.HasExportableSurface"/> für den AI-Feed-Filter ohne erneutes Parsen).
/// Erfolg mit leerem <see cref="ViewGenerationResult.OutputText"/> ist zulässig. Fehler nur bei echter Transformations- oder Validierungslogik.
/// </para>
/// <para>
/// <strong>Kein SemanticModel</strong> in der Basissignatur — nur Syntax; semantische Rewriter können später einen erweiterten Kontext erhalten.
/// </para>
/// </remarks>
public interface IViewGenerator
{
    /// <summary>Stabiler Schlüssel (Ordner-/Konzeptname), z. B. <c>complete</c>, <c>signatures-only</c>.</summary>
    string ViewKey { get; }

    ExtractionResult<ViewGenerationResult> Generate(CompilationUnitSyntax root, ViewGeneratorContext context);
}
