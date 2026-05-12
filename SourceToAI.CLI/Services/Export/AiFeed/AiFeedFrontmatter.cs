namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Metadaten für das YAML-Frontmatter des AI-Feeds (DTO; kein YAML-String).
/// </summary>
/// <param name="FeedType">Immer <see cref="DefaultFeedType"/> für diesen Export.</param>
/// <param name="Project">Anzeige gemäß Konzept: <c>SolutionName (ProjektName)</c> — Wert ohne YAML-Quotes (Escaping erfolgt später im Composer).</param>
/// <param name="SessionId">Neue Guid pro generiertem Dokument.</param>
/// <param name="Generated">Zeitstempel; ISO-8601-Formatierung erfolgt im Composer.</param>
/// <param name="FileCount">Anzahl der Manifestzeilen = enthaltene Dateien.</param>
public sealed record AiFeedFrontmatter(
    string FeedType,
    string Project,
    Guid SessionId,
    DateTimeOffset Generated,
    int FileCount)
{
    public const string DefaultFeedType = "source_export";

    /// <summary>
    /// Baut den <c>project</c>-Feldwert exakt nach Konzept-Syntax: <c>{solutionDisplayName} ({projectDisplayName})</c>.
    /// </summary>
    public static string FormatProjectField(string solutionDisplayName, string projectDisplayName) =>
        $"{solutionDisplayName} ({projectDisplayName})";

    /// <summary>
    /// <paramref name="FileCount"/> entspricht der Anzahl der Einträge in <paramref name="manifestLines"/>.
    /// </summary>
    public static AiFeedFrontmatter Create(
        string solutionDisplayName,
        string projectDisplayName,
        Guid sessionId,
        DateTimeOffset generated,
        IReadOnlyList<AiFeedManifestLine>? manifestLines) =>
        new(
            DefaultFeedType,
            FormatProjectField(solutionDisplayName, projectDisplayName),
            sessionId,
            generated,
            GetFileCount(manifestLines));

    /// <summary>
    /// Leere Liste → 0; sonst Anzahl der Manifestzeilen.
    /// </summary>
    public static int GetFileCount(IReadOnlyList<AiFeedManifestLine>? manifestLines) =>
        manifestLines?.Count ?? 0;
}
