namespace SourceToAI.CLI.Services.Processing;

/// <summary>
/// Einheitliche Anzeigereihenfolge für Manifest und Feed-Inhalt (nach absolutem Pfad, ordnernah).
/// </summary>
internal static class FeedFileDisplayOrder
{
    internal static List<string> SortByPath(IEnumerable<string> absoluteFilePaths) =>
        absoluteFilePaths
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
