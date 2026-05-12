using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Export.AiFeed;
using SourceToAI.CLI.Services.Processing;

namespace SourceToAI.CLI.Services.Processing.Markdown;

/// <summary>
/// Inhaltssegmente pro Projekt und View-Key (Parse Once über <see cref="ICSharpDocumentLoader"/>); fertiges Markdown baut <see cref="IAiFeedMarkdownComposer"/>.
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
/// Segmente ohne exportierbaren Inhalt (Task 05) werden vor der Rückgabe entfernt — siehe <see cref="AiFeedSegmentExportability"/>.
/// </remarks>
public abstract class MarkdownProjectViewBuilderBase(
    ICSharpDocumentLoader csharpDocumentLoader,
    IFileTypeService fileTypeService,
    IEnumerable<IViewGenerator> viewGenerators,
    string viewKey,
    bool includeNonCSharpFiles,
    bool passOriginalSourceTextForCSharp) : IMarkdownProjectViewBuilder
{
    private readonly IViewGenerator _viewGenerator = viewGenerators.Single(g => g.ViewKey == viewKey);

    public string ViewKey => viewKey;

    public ExtractionResult<IReadOnlyList<AiFeedContentSegment>> BuildContentSegments(
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
                return ExtractionResult<IReadOnlyList<AiFeedContentSegment>>.Failure(parseResult.ErrorMessage!);

            var parsedCSharpByFullPath = parseResult.Value!.ToDictionary(
                d => Path.GetFullPath(d.AbsolutePath),
                d => d,
                StringComparer.OrdinalIgnoreCase);

            var segments = new List<AiFeedContentSegment>();

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
                    {
                        return ExtractionResult<IReadOnlyList<AiFeedContentSegment>>.Failure(
                            gen.ErrorMessage ?? $"View {viewKey} für {relativePath} fehlgeschlagen.");
                    }

                    segments.Add(new AiFeedContentSegment(
                        relativePath,
                        "Code",
                        "csharp",
                        gen.Value!.OutputText,
                        gen.Value.HasExportableSurface));
                    continue;
                }

                if (!includeNonCSharpFiles)
                    continue;

                var content = File.ReadAllText(fullPath);
                var (typeCategory, language) = fileTypeService.GetFileTypeAndLanguage(extension);
                segments.Add(new AiFeedContentSegment(relativePath, typeCategory, language, content));
            }

            var kind = passOriginalSourceTextForCSharp
                ? AiFeedTransformedContentKind.OriginalAsTransformed
                : AiFeedTransformedContentKind.RewrittenViewOutput;

            return ExtractionResult<IReadOnlyList<AiFeedContentSegment>>.Success(
                AiFeedSegmentExportability.FilterToExportableList(segments, kind));
        }
        catch (Exception ex)
        {
            return ExtractionResult<IReadOnlyList<AiFeedContentSegment>>.Failure(
                $"Markdown-View {viewKey} für {project.ProjectName}: {ex.Message}");
        }
    }
}
