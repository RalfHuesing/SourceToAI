using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Export;

/// <summary>
/// Schreibt die zusammengeführten Multi-View-Markdown-Dateien (ein Baum pro Solution-Lauf).
/// </summary>
public interface IMultiViewExportService
{
    /// <summary>
    /// Für jede registrierte View wird pro Projekt (und ggf. virtuellem „.Docs“-Projekt in <c>complete</c>)
    /// die Markdown-Datei zweimal geschrieben: einmal im Ordner <see cref="MultiViewExportPaths.IsolatedFolderName"/>
    /// und einmal im Ordner <see cref="MultiViewExportPaths.MergedFolderName"/> (jeweils im view-spezifischen Unterordner).
    /// Der Dateiname enthält das View-Suffix (<c>SolutionName.ProjektName-viewKey.md</c>, siehe <see cref="MultiViewExportPaths"/>).
    /// </summary>
    /// <param name="outputRoot">Der globale Export-Pfad, unter dem Isolated und Merged angelegt werden.</param>
    /// <param name="solutionDisplayName">Der bereinigte Name der Solution für den Unterordner in Isolated.</param>
    /// <param name="solutionRootPath">Der Pfad zum Verzeichnis der Solution.</param>
    /// <param name="sessionId">Gemeinsame Session für alle Dateien dieses Laufs (Frontmatter).</param>
    /// <param name="generated">Zeitstempel für Frontmatter (z. B. identisch mit Readme-Lauf).</param>
    /// <param name="projectsWithFiles">Zu exportierende Projekte und deren Dateien.</param>
    /// <param name="solutionDocumentationAbsolutePaths">Optionale Lösungsdokumentation.</param>
    /// <exception cref="SourceToAI.CLI.App.Exceptions.SourceToAiExportException">Bei Build-, Compose- oder I/O-Fehlern.</exception>
    void WriteMergedSolutionViews(
        string outputRoot,
        string solutionDisplayName,
        string solutionRootPath,
        Guid sessionId,
        DateTimeOffset generated,
        IReadOnlyList<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)> projectsWithFiles,
        IReadOnlyList<string>? solutionDocumentationAbsolutePaths);
}
