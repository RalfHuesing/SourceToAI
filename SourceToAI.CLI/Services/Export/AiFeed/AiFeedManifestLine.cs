namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Eine Zeile der MANIFEST-Tabelle im AI-Feed (DTO ohne Markdown-Rendering).
/// </summary>
/// <param name="Id">Fortlaufende ID (Anker im Dokument).</param>
/// <param name="Type">Code oder Doc laut Konzept.</param>
/// <param name="Hash">Erste 8 Hex-Zeichen des MD5 (siehe <see cref="AiFeedContentHash"/>).</param>
/// <param name="Size">Byteanzahl der exportierten Zeichenkette in UTF-8 (siehe Projekt-Konzept Task 02).</param>
/// <param name="Path">Relativ zum Projektroot; Trenner im Export siehe <see cref="AiFeedManifestPath"/>.</param>
public sealed record AiFeedManifestLine(
    int Id,
    AiFeedManifestEntryType Type,
    string Hash,
    long Size,
    string Path);
