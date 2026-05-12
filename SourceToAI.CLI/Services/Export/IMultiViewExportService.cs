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
    ExtractionResult<bool> WriteMergedSolutionViews(
        string outputRoot,
        string solutionDisplayName,
        string solutionRootPath,
        IReadOnlyList<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)> projectsWithFiles,
        IReadOnlyList<string>? solutionDocumentationAbsolutePaths);
}
