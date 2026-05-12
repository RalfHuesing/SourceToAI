using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
/// <see cref="AiFeedContentSegment.FenceLanguage"/> <c>csharp</c>: transformierter Text wird geparst; enthält die
/// Oberfläche keine Typen/Enums/Delegates und keine Top-Level-Statements, entfällt das Segment (z. B. leere
/// Namespace-Hülle nach <c>public-only</c>). Keine neue Eingangs-Filterheuristik — nur Auswertung des View-Ergebnisses.
/// Bei Parser-Fehlern wird aus Vorsicht exportiert.</description></item>
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

        return CSharpTransformedHasExportableSurface(segment.TransformedText);
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

    private static bool CSharpTransformedHasExportableSurface(string transformedText)
    {
        var tree = CSharpSyntaxTree.ParseText(
            transformedText,
            CSharpParseOptions.Default,
            path: "view-segment.cs");

        if (tree.GetDiagnostics().Any(static d => d.Severity == DiagnosticSeverity.Error))
            return true;

        var root = tree.GetCompilationUnitRoot();
        return root.DescendantNodes().Any(static n =>
            n is BaseTypeDeclarationSyntax
                or EnumDeclarationSyntax
                or DelegateDeclarationSyntax
                or GlobalStatementSyntax);
    }
}
