using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Export;

/// <summary>
/// Schreibt die zusammengeführten Multi-View-Markdown-Dateien (ein Baum pro Solution-Lauf).
/// </summary>
public interface IMultiViewExportService
{
    /// <summary>
    /// Für jede registrierte View wird pro Projekt (und ggf. virtuellem „.Docs“-Projekt in <c>complete</c>)
    /// eine Markdown-Datei unter <see cref="MultiViewExportPaths.GetViewFolderNameForViewKey"/> geschrieben
    /// (<c>SolutionName.ProjektName.md</c>, siehe <see cref="MultiViewExportPaths"/>).
    /// </summary>
    /// <param name="sessionId">Gemeinsame Session für alle Dateien dieses Laufs (Frontmatter).</param>
    /// <param name="generated">Zeitstempel für Frontmatter (z. B. identisch mit Readme-Lauf).</param>
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
