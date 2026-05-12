using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Processing;

/// <summary>
/// Liest alle vorkommenden <c>.cs</c>-Pfade aus der gegebenen Reihenfolge je Datei höchstens einmal
/// ein und parst sie mit Roslyn.
/// </summary>
public interface ICSharpDocumentLoader
{
    /// <summary>
    /// Liefert pro <strong>einzigartigem</strong> <c>.cs</c>-Pfad (case-insensitive) genau ein
    /// <see cref="ParsedCSharpDocument"/> in der Reihenfolge des ersten Auftretens in
    /// <paramref name="absoluteFilePathsInDisplayOrder"/>.
    /// </summary>
    ExtractionResult<IReadOnlyList<ParsedCSharpDocument>> LoadParsedDocuments(
        ProjectDefinition project,
        IReadOnlyList<string> absoluteFilePathsInDisplayOrder);
}
