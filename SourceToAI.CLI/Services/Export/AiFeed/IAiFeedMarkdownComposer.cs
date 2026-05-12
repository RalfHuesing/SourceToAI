namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Baut das fertige AI-Feed-Markdown (Frontmatter A, Header/Instruction B, MANIFEST C, CONTENT D) gemäß Projektkonzept.
/// </summary>
public interface IAiFeedMarkdownComposer
{
    /// <summary>
    /// Erzeugt ein Dokument aus Anzeigenamen, Session/Zeitstempel und den (bereits gefilterten) Segmenten.
    /// Manifestzeilen werden aus den Segmenten abgeleitet (Hash/Size/Type/Pfad).
    /// </summary>
    string Compose(
        string solutionDisplayName,
        string projectDisplayName,
        Guid sessionId,
        DateTimeOffset generated,
        IReadOnlyList<AiFeedContentSegment> segments);
}
