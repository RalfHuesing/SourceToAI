using System.Text;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.IO;
using SourceToAI.CLI.Services.Processing;

namespace SourceToAI.CLI.Services.Processing.Markdown;

/// <summary>
/// Markdown-Aggregation pro Projekt und View-Key (Parse Once über <see cref="ICSharpDocumentLoader"/>).
/// </summary>
/// <remarks>
/// Rewriter-Reihenfolge je View (Task 06, Abgleich Konzept):
/// <list type="bullet">
/// <item><description><b>complete</b> — keine Rewriter; C#-Text = <see cref="ParsedCSharpDocument.SourceText"/> (über <see cref="ViewGeneratorContext.OriginalSourceText"/>).</description></item>
/// <item><description><b>signatures-only</b> — nur <c>SignaturesRewriter</c> (<c>SignaturesOnlyViewGenerator</c>).</description></item>
/// <item><description><b>public-only</b> — nur <c>VisibilityRewriter</c> (<c>PublicOnlyViewGenerator</c>); Bodies öffentlicher Member bleiben, kein <c>SignaturesRewriter</c>.</description></item>
/// <item><description><b>dto-only</b> — nur <c>DtoRewriter</c> (<c>DtoOnlyViewGenerator</c>).</description></item>
/// </list>
/// Nicht-<c>.cs</c>-Dateien erscheinen nur in <b>complete</b> (wie Konzept „alles 1:1“); andere Views nur <c>.cs</c>.
/// </remarks>
public abstract class MarkdownProjectViewBuilderBase(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileReader fileReader,
    IFileTypeService fileTypeService,
    IEnumerable<IViewGenerator> viewGenerators,
    string viewKey,
    string relativeOutputFile,
    bool includeNonCSharpFiles,
    bool passOriginalSourceTextForCSharp) : IMarkdownProjectViewBuilder
{
    private readonly IViewGenerator _viewGenerator = viewGenerators.Single(g => g.ViewKey == viewKey);

    public string ViewKey => viewKey;

    public string RelativeOutputFile => relativeOutputFile;

    public ExtractionResult<string> BuildMarkdown(
        ProjectDefinition project,
        IReadOnlyList<string> absoluteFilePathsInDisplayOrder)
    {
        try
        {
            var sortedPaths = absoluteFilePathsInDisplayOrder
                .OrderByDescending(p => Path.GetExtension(p).Equals(".md", StringComparison.OrdinalIgnoreCase))
                .ThenBy(p => p)
                .ToList();

            var parseResult = csharpDocumentLoader.LoadParsedDocuments(project, sortedPaths);
            if (!parseResult.IsSuccess)
                return ExtractionResult<string>.Failure(parseResult.ErrorMessage!);

            var parsedCSharpByFullPath = parseResult.Value!.ToDictionary(
                d => Path.GetFullPath(d.AbsolutePath),
                d => d,
                StringComparer.OrdinalIgnoreCase);

            var sb = new StringBuilder();

            foreach (var path in sortedPaths)
            {
                var fullPath = Path.GetFullPath(path);
                var relativePath = Path.GetRelativePath(project.RootDirectory, fullPath);
                var extension = Path.GetExtension(fullPath);

                if (parsedCSharpByFullPath.TryGetValue(fullPath, out var parsed))
                {
                    var ctx = passOriginalSourceTextForCSharp
                        ? new ViewGeneratorContext(relativePath, parsed.SourceText)
                        : new ViewGeneratorContext(relativePath);

                    var gen = _viewGenerator.Generate(parsed.Root, ctx);
                    if (!gen.IsSuccess)
                        return ExtractionResult<string>.Failure(gen.ErrorMessage ?? $"View {viewKey} für {relativePath} fehlgeschlagen.");

                    AppendSection(sb, relativePath, gen.Value!, "csharp");
                    continue;
                }

                if (!includeNonCSharpFiles)
                    continue;

                var content = fileReader.ReadAllText(fullPath);
                var (_, language) = fileTypeService.GetFileTypeAndLanguage(extension);
                AppendSection(sb, relativePath, content, language);
            }

            return ExtractionResult<string>.Success(sb.ToString());
        }
        catch (Exception ex)
        {
            return ExtractionResult<string>.Failure(
                $"Markdown-View {viewKey} für {project.ProjectName}: {ex.Message}");
        }
    }

    private static void AppendSection(StringBuilder sb, string relativePath, string content, string language)
    {
        sb.AppendLine("---");
        sb.AppendLine($"### {relativePath}");

        int requiredBackticks = MarkdownFenceUtility.CalculateRequiredBackticks(content);
        string fence = new string('`', requiredBackticks);

        sb.AppendLine($"{fence}{language}");
        sb.AppendLine(content);
        sb.AppendLine(fence);
        sb.AppendLine();
    }
}
