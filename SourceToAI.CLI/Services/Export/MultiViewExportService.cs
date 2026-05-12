using System.Linq;
using System.Text;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Processing.Markdown;

namespace SourceToAI.CLI.Services.Export;

public sealed class MultiViewExportService(IEnumerable<IMarkdownProjectViewBuilder> viewBuilders) : IMultiViewExportService
{
    private static readonly string[] ViewKeyOrder = ["complete", "signatures-only", "public-only", "dto-only"];

    public ExtractionResult<bool> WriteMergedSolutionViews(
        string outputRoot,
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

                var aggregate = new StringBuilder();

                if (viewKey.Equals("complete", StringComparison.Ordinal)
                    && solutionDocumentationAbsolutePaths is { Count: > 0 })
                {
                    var docProject = new ProjectDefinition(".Docs", Path.Combine(solutionRootPath, "virtual.csproj"));
                    var docResult = builder.BuildMarkdown(docProject, solutionDocumentationAbsolutePaths);
                    if (!docResult.IsSuccess)
                        return ExtractionResult<bool>.Failure(docResult.ErrorMessage!);

                    aggregate.AppendLine("## Solution-Dokumentation (.Docs)");
                    aggregate.AppendLine();
                    aggregate.Append(docResult.Value);
                    aggregate.AppendLine();
                }

                foreach (var (project, paths) in projectsWithFiles.OrderBy(p => p.Project.ProjectName, StringComparer.OrdinalIgnoreCase))
                {
                    if (paths.Count == 0)
                        continue;

                    var part = builder.BuildMarkdown(project, paths);
                    if (!part.IsSuccess)
                        return ExtractionResult<bool>.Failure(part.ErrorMessage!);

                    aggregate.AppendLine($"## Projekt: {project.ProjectName}");
                    aggregate.AppendLine();
                    aggregate.Append(part.Value);
                    aggregate.AppendLine();
                }

                var outPath = Path.Combine(outputRoot, builder.RelativeOutputFile);
                var outDir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(outDir))
                    Directory.CreateDirectory(outDir);

                var body = aggregate.Length == 0
                    ? "*(Keine passenden Dateien für diese Sicht.)*\n"
                    : aggregate.ToString();
                File.WriteAllText(outPath, body);
            }

            return ExtractionResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ExtractionResult<bool>.Failure($"Multi-View-Export: {ex.Message}");
        }
    }
}
