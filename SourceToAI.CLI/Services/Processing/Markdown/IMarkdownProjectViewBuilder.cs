using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Export.AiFeed;
using SourceToAI.CLI.Services.Processing;

namespace SourceToAI.CLI.Services.Processing.Markdown;

/// <summary>
/// Erzeugt gefilterte Inhaltssegmente für eine Code-View eines Projekts; Layout (Frontmatter, MANIFEST, CONTENT) liefert <see cref="IAiFeedMarkdownComposer"/>.
/// </summary>
public interface IMarkdownProjectViewBuilder
{
    string ViewKey { get; }

    /// <summary>Relativ zum Export-Projektordner, z. B. <c>complete/full-source.md</c>.</summary>
    string RelativeOutputFile { get; }

    ExtractionResult<IReadOnlyList<AiFeedContentSegment>> BuildContentSegments(
        ProjectDefinition project,
        IReadOnlyList<string> absoluteFilePathsInDisplayOrder);
}
