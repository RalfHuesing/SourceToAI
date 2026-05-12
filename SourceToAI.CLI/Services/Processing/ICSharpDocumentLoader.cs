using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Processing;

/// <summary>
/// Liest alle vorkommenden <c>.cs</c>-Pfade aus der gegebenen Reihenfolge je Datei höchstens einmal
/// ein und parst sie mit Roslyn. Implementierungen können Parse-Ergebnisse pro normalisiertem absoluten
/// Pfad über mehrere Aufrufe und Views hinweg zwischenspeichern (<c>Parse Once</c>).
/// </summary>
public interface ICSharpDocumentLoader
{
    /// <summary>
    /// Verwirft alle zwischengespeicherten Parse-Einträge (z. B. zu Beginn eines Exportlaufs oder in Tests).
    /// </summary>
    void Clear();

    /// <summary>
    /// Liefert pro <strong>einzigartigem</strong> <c>.cs</c>-Pfad (case-insensitive) genau ein
    /// <see cref="ParsedCSharpDocument"/> in der Reihenfolge des ersten Auftretens in
    /// <paramref name="absoluteFilePathsInDisplayOrder"/>.
    /// </summary>
    ExtractionResult<IReadOnlyList<ParsedCSharpDocument>> LoadParsedDocuments(
        ProjectDefinition project,
        IReadOnlyList<string> absoluteFilePathsInDisplayOrder);
}
