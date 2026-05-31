using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SourceToAI.Tests.App;

/// <summary>
/// Gemeinsame Assertions für AI-Feed-Export-Integrationstests (Struktur, Signaturen).
/// </summary>
public static class AiFeedExportIntegrationAsserts
{
    private static readonly Regex FrontmatterClosedBeforeHeader = new(
        @"\A---\r?\n[\s\S]*?\r?\n---\r?\n(?:\r?\n)*# AI FEED:",
        RegexOptions.CultureInvariant);

    private static readonly Regex ManifestDataRow = new(
        @"^\| \[\d+\] \|",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly Regex ContentSectionHeading = new(
        @"^### \[\d+\] ",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    /// <summary>
    /// Extrahiert Inhalte aus <c>```…csharp</c>-Blöcken (variable Fence-Länge wie in der CLI).
    /// </summary>
    public static IEnumerable<string> ExtractCSharpFenceContents(string markdown)
    {
        using var reader = new StringReader(markdown);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var m = Regex.Match(line, @"^(?<fence>`{3,})csharp\s*$");
            if (!m.Success)
                continue;

            var fence = m.Groups["fence"].Value;
            var sb = new StringBuilder();
            while ((line = reader.ReadLine()) != null)
            {
                if (line == fence)
                {
                    var code = sb.ToString().TrimEnd('\r', '\n');
                    if (!string.IsNullOrWhiteSpace(code))
                        yield return code;
                    break;
                }

                sb.AppendLine(line);
            }
        }
    }

    /// <summary>
    /// Jeder extrahierte Signatur-Block muss als C# ohne Parser-Fehler durchgehen.
    /// </summary>
    public static void AssertSignatureFencesParseWithoutSyntaxErrors(string signaturesMarkdown)
    {
        var blocks = ExtractCSharpFenceContents(signaturesMarkdown).ToList();
        Assert.NotEmpty(blocks);
        foreach (var code in blocks)
        {
            var tree = CSharpSyntaxTree.ParseText(
                code,
                CSharpParseOptions.Default,
                path: "signatures-fragment.cs",
                encoding: Encoding.UTF8);
            var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
        }
    }

    /// <summary>
    /// YAML-Frontmatter, MANIFEST/CONTENT, Manifest-ID <c>[n]</c>, Zeilenanzahl Manifest = Anzahl CONTENT-Anker.
    /// </summary>
    public static void AssertAiFeedStructuralInvariants(string markdown)
    {
        Assert.True(
            FrontmatterClosedBeforeHeader.IsMatch(markdown),
            "Erwartet: YAML-Frontmatter (--- … ---) unmittelbar vor # AI FEED:.");

        Assert.Contains("## MANIFEST", markdown, StringComparison.Ordinal);
        Assert.Contains("## CONTENT", markdown, StringComparison.Ordinal);

        var manifestIdx = markdown.IndexOf("## MANIFEST", StringComparison.Ordinal);
        var contentIdx = markdown.IndexOf("## CONTENT", StringComparison.Ordinal);
        Assert.True(manifestIdx >= 0 && contentIdx > manifestIdx);

        var manifestSlice = markdown.Substring(manifestIdx, contentIdx - manifestIdx);
        var manifestRows = ManifestDataRow.Matches(manifestSlice).Count;

        var contentSlice = markdown.Substring(contentIdx);
        var contentHeadings = ContentSectionHeading.Matches(contentSlice).Count;

        Assert.Equal(manifestRows, contentHeadings);
    }
}
