namespace SourceToAI.CLI.Services.Export;

/// <summary>
/// Erzeugt Markdown-Hilfen für den Export: eine globale <c>readme.md</c> unter dem Export-Wurzelverzeichnis
/// und pro Lösung eine <c>readme.md</c> unter <c>Isolated/&lt;Solution&gt;/</c>.
/// </summary>
public interface IMultiViewReadmeMarkdownGenerator
{
    /// <summary>
    /// Globale Orientierung für KI-Nutzer: Aufbau von <c>Isolated/</c> und <c>Merged/</c>, Views, Suchreihenfolge (Signaturen vor <c>complete</c>), <c>rg</c>/<c>grep</c>.
    /// </summary>
    string GenerateGlobalExportOverview(DateTimeOffset generatedAtUtc);

    /// <summary>
    /// Lösungsspezifische Details (MANIFEST/CONTENT, Pfade relativ zu <c>Isolated/&lt;Solution&gt;/</c>).
    /// </summary>
    /// <param name="solutionDisplayName">Anzeigename der Solution (Ordner unter <c>Isolated/</c>).</param>
    /// <param name="repositoryRootFolderName">Quell- bzw. Anzeigeordnername laut Konzept (z. B. Repo- oder Assembly-Ebene).</param>
    string GenerateIsolatedSolutionReadme(
        string solutionDisplayName,
        string repositoryRootFolderName,
        DateTimeOffset generatedAtUtc);
}
