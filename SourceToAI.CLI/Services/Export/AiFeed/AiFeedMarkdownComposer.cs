using System.Globalization;
using System.Text;
using SourceToAI.CLI.Services.Processing.Markdown;

namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>Zentraler Aufbau des AI-Feed-Markdowns (einzige Stelle für Layout A–D).</summary>
public sealed class AiFeedMarkdownComposer : IAiFeedMarkdownComposer
{
    public string Compose(
        string solutionDisplayName,
        string projectDisplayName,
        Guid sessionId,
        DateTimeOffset generated,
        IReadOnlyList<AiFeedContentSegment> segments)
    {
        ArgumentNullException.ThrowIfNull(solutionDisplayName);
        ArgumentNullException.ThrowIfNull(projectDisplayName);
        ArgumentNullException.ThrowIfNull(segments);

        segments = AiFeedSegmentExportability.FilterToExportableList(
            segments,
            AiFeedTransformedContentKind.OriginalAsTransformed);
        var manifestLines = BuildManifestLines(segments);
        var frontmatter = AiFeedFrontmatter.Create(
            solutionDisplayName,
            projectDisplayName,
            sessionId,
            generated,
            manifestLines);

        var displayTitle = AiFeedFrontmatter.FormatProjectField(solutionDisplayName, projectDisplayName);
        var sb = new StringBuilder();

        AppendYamlFrontmatter(sb, frontmatter);
        sb.AppendLine();
        AppendHeaderAndInstruction(sb, displayTitle, projectDisplayName);
        sb.AppendLine();
        AppendManifest(sb, manifestLines);
        sb.AppendLine();
        AppendContent(sb, segments, manifestLines);

        return sb.ToString();
    }

    private static List<AiFeedManifestLine> BuildManifestLines(IReadOnlyList<AiFeedContentSegment> segments)
    {
        var list = new List<AiFeedManifestLine>(segments.Count);
        for (var i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            ArgumentNullException.ThrowIfNull(seg.RelativePathFromProjectRoot);
            ArgumentNullException.ThrowIfNull(seg.FileTypeCategory);
            ArgumentNullException.ThrowIfNull(seg.FenceLanguage);
            ArgumentNullException.ThrowIfNull(seg.TransformedText);

            var id = i + 1;
            var path = AiFeedManifestPath.NormalizeForManifestTable(seg.RelativePathFromProjectRoot);
            var size = AiFeedExportedUtf8Size.OfExportedString(seg.TransformedText);
            var type = AiFeedManifestEntryTypeMapping.FromFileTypeCategory(seg.FileTypeCategory);
            list.Add(new AiFeedManifestLine(id, type, size, path));
        }

        return list;
    }

    private static void AppendYamlFrontmatter(StringBuilder sb, AiFeedFrontmatter fm)
    {
        sb.AppendLine("---");
        sb.AppendLine($"feed_type: {fm.FeedType}");
        sb.AppendLine($"project: \"{YamlDoubleQuotedEscaping.EscapeYamlDoubleQuoted(fm.Project)}\"");
        sb.AppendLine($"session_id: {fm.SessionId:D}");
        sb.AppendLine($"generated: \"{fm.Generated.ToString("O", CultureInfo.InvariantCulture)}\"");
        sb.AppendLine($"file_count: {fm.FileCount}");
        sb.AppendLine("---");
    }

    private static void AppendHeaderAndInstruction(StringBuilder sb, string displayTitle, string projectDisplayName)
    {
        sb.AppendLine($"# AI FEED: {displayTitle}");
        sb.AppendLine();
        sb.AppendLine("## INSTRUCTION");
        sb.AppendLine(
            $"SYSTEM-KONTEXT: Dies ist ein Snapshot eines Software-Projekts. Das Format ist Markdown mit Fencing. Dies ist Projekt: '{projectDisplayName}'. Analysiere den Code im Kontext der Architektur.");
    }

    private static void AppendManifest(StringBuilder sb, IReadOnlyList<AiFeedManifestLine> manifestLines)
    {
        sb.AppendLine("## MANIFEST");
        sb.AppendLine("| ID | Type | Size | Path |");
        sb.AppendLine("|---:|:---|---:|:---|");
        foreach (var line in manifestLines)
        {
            var typeLabel = line.Type == AiFeedManifestEntryType.Doc ? "Doc" : "Code";
            sb.AppendLine($"| [{line.Id}] | {typeLabel} | {line.Size} | {line.Path} |");
        }
    }

    private static void AppendContent(
        StringBuilder sb,
        IReadOnlyList<AiFeedContentSegment> segments,
        IReadOnlyList<AiFeedManifestLine> manifestLines)
    {
        sb.AppendLine("## CONTENT");

        if (segments.Count == 0)
            return;

        for (var i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            var path = manifestLines[i].Path;

            sb.AppendLine($"### [{i + 1}] {path}");

            var required = MarkdownFenceUtility.CalculateRequiredBackticks(seg.TransformedText);
            var fence = new string('`', required);
            sb.AppendLine($"{fence}{seg.FenceLanguage}");
            sb.AppendLine(seg.TransformedText);
            sb.AppendLine(fence);
        }
    }
}
