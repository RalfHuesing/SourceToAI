namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Steuert, wie streng <see cref="AiFeedSegmentExportability"/> C#-Segmente bewertet — abhängig davon, ob
/// <see cref="AiFeedContentSegment.TransformedText"/> Rohtext (complete) oder Rewriter-Ausgabe ist.
/// </summary>
public enum AiFeedTransformedContentKind
{
    /// <summary>
    /// <see cref="MarkdownProjectViewBuilderBase"/> mit <c>passOriginalSourceTextForCSharp: true</c>:
    /// nur Whitespace/Leerstring entfernen; Kommentar-only-Fragmente bleiben exportierbar.
    /// </summary>
    OriginalAsTransformed,

    /// <summary>
    /// View-Ausgabe nach Roslyn-Rewritern: zusätzlich C# ohne exportierbare Syntax (z. B. nur <c>namespace N;</c>-Hülle) entfernen.
    /// </summary>
    RewrittenViewOutput,
}
