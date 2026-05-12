namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Entscheidet, ob ein Inhaltssegment im AI-Feed erscheint (gemeinsame Quelle für <c>## MANIFEST</c> und <c>## CONTENT</c>).
/// </summary>
/// <remarks>
/// <para><b>Leere Dateien nach View-Filter (Task 05):</b></para>
/// <list type="bullet">
/// <item>
/// <description>Immer: <see cref="AiFeedContentSegment.TransformedText"/> nur aus Whitespace / leer
/// (<see cref="string.IsNullOrWhiteSpace"/>) → kein Manifest, kein CONTENT-Block.</description></item>
/// <item>
/// <description>Zusätzlich bei <see cref="AiFeedTransformedContentKind.RewrittenViewOutput"/> und
/// <see cref="AiFeedContentSegment.FenceLanguage"/> <c>csharp</c>: es wird
/// <see cref="AiFeedContentSegment.CSharpRewrittenHasExportableSurface"/> ausgewertet (vom View-Generator auf dem
/// umgeschriebenen AST gesetzt, ohne erneutes Parsen). Fehlt das Flag, wird eine Ausnahme geworfen. Enthält die
/// Oberfläche keine Typen/Enums/Delegates und keine Top-Level-Statements, entfällt das Segment (z. B. leere
/// Namespace-Hülle nach <c>public-only</c>).</description></item>
/// <item>
/// <description><see cref="AiFeedTransformedContentKind.OriginalAsTransformed"/> (complete-Ansicht): keine
/// syntaktische „Hüllen“-Prüfung, damit z. B. Kommentar-only-<c>.cs</c>-Dateien erhalten bleiben.</description></item>
/// </list>
/// </remarks>
public static class AiFeedSegmentExportability
{
    public static bool IsExportable(AiFeedContentSegment segment, AiFeedTransformedContentKind kind)
    {
        ArgumentNullException.ThrowIfNull(segment);
        ArgumentNullException.ThrowIfNull(segment.TransformedText);

        if (string.IsNullOrWhiteSpace(segment.TransformedText))
            return false;

        if (kind == AiFeedTransformedContentKind.OriginalAsTransformed)
            return true;

        if (!segment.FenceLanguage.Equals("csharp", StringComparison.OrdinalIgnoreCase))
            return true;

        if (segment.CSharpRewrittenHasExportableSurface is not bool surface)
        {
            throw new InvalidOperationException(
                $"C#-Segment ohne gesetztes {nameof(AiFeedContentSegment.CSharpRewrittenHasExportableSurface)} bei " +
                $"{nameof(AiFeedTransformedContentKind.RewrittenViewOutput)}: {segment.RelativePathFromProjectRoot}");
        }

        return surface;
    }

    /// <summary>Materialisiert nur exportierbare Segmente (Reihenfolge unverändert, IDs danach lückenlos 1…k).</summary>
    public static List<AiFeedContentSegment> FilterToExportableList(
        IReadOnlyList<AiFeedContentSegment> segments,
        AiFeedTransformedContentKind kind)
    {
        ArgumentNullException.ThrowIfNull(segments);
        var list = new List<AiFeedContentSegment>(segments.Count);
        foreach (var s in segments)
        {
            if (IsExportable(s, kind))
                list.Add(s);
        }

        return list;
    }
}
