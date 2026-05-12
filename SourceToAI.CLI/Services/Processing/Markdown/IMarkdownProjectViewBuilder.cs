using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Processing;

namespace SourceToAI.CLI.Services.Processing.Markdown;

/// <summary>
/// Erzeugt eine Markdown-Datei für eine Code-View eines Projekts (Pfad-Sections + dynamische Fences).
/// </summary>
public interface IMarkdownProjectViewBuilder
{
    string ViewKey { get; }

    /// <summary>Relativ zum Export-Projektordner, z. B. <c>complete/full-source.md</c>.</summary>
    string RelativeOutputFile { get; }

    ExtractionResult<string> BuildMarkdown(
        ProjectDefinition project,
        IReadOnlyList<string> absoluteFilePathsInDisplayOrder);
}
