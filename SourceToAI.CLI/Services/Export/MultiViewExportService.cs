using System.Linq;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Export.AiFeed;
using SourceToAI.CLI.Services.Processing.Markdown;
namespace SourceToAI.CLI.Services.Export;

public sealed class MultiViewExportService(
    IEnumerable<IMarkdownProjectViewBuilder> viewBuilders,
    IAiFeedMarkdownComposer markdownComposer) : IMultiViewExportService
{
    private static readonly string[] ViewKeyOrder = ["complete", "signatures-only", "public-only", "dto-only"];

    public ExtractionResult<bool> WriteMergedSolutionViews(
        string outputRoot,
        string solutionDisplayName,
        string solutionRootPath,
        Guid sessionId,
        DateTimeOffset generated,
        IReadOnlyList<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)> projectsWithFiles,
        IReadOnlyList<string>? solutionDocumentationAbsolutePaths)
    {
        try
        {
            var buildersByKey = viewBuilders.ToDictionary(b => b.ViewKey, StringComparer.Ordinal);
            var usedStemsPerView = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var vk in ViewKeyOrder)
                usedStemsPerView[vk] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var orderedProjects = projectsWithFiles
                .OrderBy(p => p.Project.ProjectName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var exportUnits = new List<(ProjectDefinition Project, IReadOnlyList<string> Paths, bool DocsOnlyInCompleteView)>();
            if (solutionDocumentationAbsolutePaths is { Count: > 0 })
            {
                var docProject = new ProjectDefinition(".Docs", Path.Combine(solutionRootPath, "virtual.csproj"));
                exportUnits.Add((docProject, solutionDocumentationAbsolutePaths, true));
            }

            foreach (var (project, paths) in orderedProjects)
            {
                if (paths.Count == 0)
                    continue;
                exportUnits.Add((project, paths, false));
            }

            foreach (var (project, paths, docsOnlyInCompleteView) in exportUnits)
            {
                foreach (var viewKey in ViewKeyOrder)
                {
                    if (docsOnlyInCompleteView && !viewKey.Equals("complete", StringComparison.Ordinal))
                        continue;

                    if (!buildersByKey.TryGetValue(viewKey, out var builder))
                        return ExtractionResult<bool>.Failure($"Kein Markdown-View-Builder für „{viewKey}“ registriert.");

                    var viewFolder = MultiViewExportPaths.GetViewFolderNameForViewKey(viewKey);
                    var usedStems = usedStemsPerView[viewKey];

                    var part = builder.BuildContentSegments(project, paths);
                    if (!part.IsSuccess)
                        return ExtractionResult<bool>.Failure(part.ErrorMessage!);

                    if (part.Value!.Count == 0)
                        continue;

                    var body = markdownComposer.Compose(
                        solutionDisplayName,
                        project.ProjectName,
                        sessionId,
                        generated,
                        part.Value!);

                    var stem = MultiViewExportPaths.AllocateUniqueFileStem(
                        MultiViewExportPaths.BuildSanitizedExportFileStem(solutionDisplayName, project.ProjectName),
                        usedStems);
                    WriteProjectViewFile(outputRoot, viewFolder, stem, body);
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
