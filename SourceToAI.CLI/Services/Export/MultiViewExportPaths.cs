namespace SourceToAI.CLI.Services.Export;

/// <summary>
/// Zielpfade für den Multi-View-Export laut <c>konzept.md</c> Abschnitt 2 — alles direkt unter
/// <c>{exportPath}/{solutionName}</c> (kein zusätzlicher Zwischenordner).
/// </summary>
public static class MultiViewExportPaths
{
    public const string CompleteFolderName = "complete";

    public const string SignaturesOnlyFolderName = "signatures-only";

    public const string PublicOnlyFolderName = "public-only";

    public const string DtoOnlyFolderName = "dto-only";

    /// <summary>
    /// Wurzelverzeichnis für <c>readme.md</c>, <c>dependency-graph.md</c> und alle View-Unterordner.
    /// Absolut: <c>Path.Combine(exportPath, solutionName)</c>.
    /// </summary>
    /// <remarks>
    /// Vor jedem Lauf vom Orchestrator vollständig leeren/neu anlegen, damit keine veralteten Dateien bleiben.
    /// </remarks>
    public static string GetSolutionExportRoot(string exportPath, string solutionName) =>
        Path.Combine(exportPath, solutionName);
}
