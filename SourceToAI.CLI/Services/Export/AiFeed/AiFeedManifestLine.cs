namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Eine Zeile der MANIFEST-Tabelle im AI-Feed (DTO ohne Markdown-Rendering).
/// </summary>
/// <param name="Id">Fortlaufende ID (Referenz zu CONTENT-Überschrift <c>### [n]</c>).</param>
/// <param name="Type">Code oder Doc laut Konzept.</param>
/// <param name="Size">Byteanzahl der exportierten Zeichenkette in UTF-8 (siehe Projekt-Konzept Task 02).</param>
/// <param name="Path">Relativ zum Projektroot; Trenner im Export siehe <see cref="AiFeedManifestPath"/>.</param>
public sealed record AiFeedManifestLine(
    int Id,
    AiFeedManifestEntryType Type,
    long Size,
    string Path);
