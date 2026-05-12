using System.Linq;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Processing.Markdown;

namespace SourceToAI.CLI.Services.Export;

public sealed class MultiViewExportService(IEnumerable<IMarkdownProjectViewBuilder> viewBuilders) : IMultiViewExportService
{
    private static readonly string[] ViewKeyOrder = ["complete", "signatures-only", "public-only", "dto-only"];

    public ExtractionResult<bool> WriteMergedSolutionViews(
        string outputRoot,
        string solutionDisplayName,
        string solutionRootPath,
        IReadOnlyList<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)> projectsWithFiles,
        IReadOnlyList<string>? solutionDocumentationAbsolutePaths)
    {
        try
        {
            var buildersByKey = viewBuilders.ToDictionary(b => b.ViewKey, StringComparer.Ordinal);

            foreach (var viewKey in ViewKeyOrder)
            {
                if (!buildersByKey.TryGetValue(viewKey, out var builder))
                    return ExtractionResult<bool>.Failure($"Kein Markdown-View-Builder für „{viewKey}“ registriert.");

                var viewFolder = MultiViewExportPaths.GetViewFolderNameForViewKey(viewKey);
                var usedStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (viewKey.Equals("complete", StringComparison.Ordinal)
                    && solutionDocumentationAbsolutePaths is { Count: > 0 })
                {
                    var docProject = new ProjectDefinition(".Docs", Path.Combine(solutionRootPath, "virtual.csproj"));
                    var docResult = builder.BuildMarkdown(docProject, solutionDocumentationAbsolutePaths);
                    if (!docResult.IsSuccess)
                        return ExtractionResult<bool>.Failure(docResult.ErrorMessage!);

                    var stem = MultiViewExportPaths.AllocateUniqueFileStem(
                        MultiViewExportPaths.BuildSanitizedExportFileStem(solutionDisplayName, docProject.ProjectName),
                        usedStems);
                    WriteProjectViewFile(outputRoot, viewFolder, stem, docResult.Value!);
                }

                foreach (var (project, paths) in projectsWithFiles.OrderBy(
                             p => p.Project.ProjectName,
                             StringComparer.OrdinalIgnoreCase))
                {
                    if (paths.Count == 0)
                        continue;

                    var part = builder.BuildMarkdown(project, paths);
                    if (!part.IsSuccess)
                        return ExtractionResult<bool>.Failure(part.ErrorMessage!);

                    var stem = MultiViewExportPaths.AllocateUniqueFileStem(
                        MultiViewExportPaths.BuildSanitizedExportFileStem(solutionDisplayName, project.ProjectName),
                        usedStems);
                    WriteProjectViewFile(outputRoot, viewFolder, stem, part.Value!);
                }
            }

            return ExtractionResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ExtractionResult<bool>.Failure($"Multi-View-Export: {ex.Message}");
        }
    }

    private static void WriteProjectViewFile(string outputRoot, string viewFolder, string uniqueStem, string body)
    {
        var outPath = MultiViewExportPaths.GetViewOutputPath(outputRoot, viewFolder, uniqueStem);
        var outDir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrEmpty(outDir))
            Directory.CreateDirectory(outDir);

        File.WriteAllText(outPath, body);
    }
}
