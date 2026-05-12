namespace SourceToAI.CLI.Services.Export;

/// <summary>
/// Zielpfade für den Multi-View-Export laut <c>konzept.md</c> (Unterordner
/// <c>complete/</c>, <c>signatures-only/</c>, …).
/// </summary>
public static class MultiViewExportPaths
{
    /// <summary>
    /// Name des Unterordners unter <c>{exportPath}/{solutionName}</c>, der die gesamte
    /// Multi-View-Baumstruktur enthält. So bleiben bestehende flache Legacy-Feeds
    /// (<c>*.md</c> mit Datumssuffix) im Lösungsordner erhalten, bis Task 08 die
    /// Orchestrierung final vereinheitlicht.
    /// </summary>
    public const string MultiViewFolderName = "multi-view";

    public const string CompleteFolderName = "complete";

    public const string SignaturesOnlyFolderName = "signatures-only";

    public const string PublicOnlyFolderName = "public-only";

    public const string DtoOnlyFolderName = "dto-only";

    /// <summary>
    /// Wurzelverzeichnis für <c>readme.md</c>, <c>dependency-graph.md</c> und alle View-Unterordner.
    /// Absolut: <c>Path.Combine(exportPath, solutionName, <see cref="MultiViewFolderName"/>)</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Clean vor Lauf (Task 08):</strong> Diesen Ordner vollständig löschen und neu anlegen,
    /// damit keine veralteten View-Dateien aus vorherigen Läufen stehen bleiben.
    /// </remarks>
    public static string GetMultiViewRoot(string exportPath, string solutionName) =>
        Path.Combine(exportPath, solutionName, MultiViewFolderName);
}
