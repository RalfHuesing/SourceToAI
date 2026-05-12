using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Export;

/// <summary>
/// Schreibt die zusammengeführten Multi-View-Markdown-Dateien (ein Baum pro Solution-Lauf).
/// </summary>
public interface IMultiViewExportService
{
    /// <summary>
    /// Für jede registrierte Markdown-View-Builder-Sicht werden alle Projekte in eine Datei
    /// (relativer Pfad je Builder) geschrieben. Solution-Dokumentation erscheint nur in der Sicht <c>complete</c>.
    /// </summary>
    ExtractionResult<bool> WriteMergedSolutionViews(
        string outputRoot,
        string solutionRootPath,
        IReadOnlyList<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)> projectsWithFiles,
        IReadOnlyList<string>? solutionDocumentationAbsolutePaths);
}
