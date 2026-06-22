using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services;
using SourceToAI.CLI.Services.Export.AiFeed;
using SourceToAI.CLI.Services.Processing.Markdown;
using System.Text;

namespace SourceToAI.CLI.Services.Processing;

public sealed class MarkdownFeedGenerator(ICSharpDocumentLoader csharpDocumentLoader) : IFeedGenerator
{
    private record LoadFilesResult(
        List<FileContent> FileContents,
        List<FileManifestEntry> Manifests);

    public ExtractionResult<string> GenerateFeed(string solutionName, ProjectDefinition project, List<string> filePaths)
    {
        try
        {
            csharpDocumentLoader.Clear();

            var feedName = $"{solutionName} ({project.ProjectName})";
            var sortedPaths = FeedFileDisplayOrder.SortByPath(filePaths);

            var parseResult = csharpDocumentLoader.LoadParsedDocuments(project, sortedPaths);
            if (!parseResult.IsSuccess)
                return ExtractionResult<string>.Failure(parseResult.ErrorMessage!);

            var warnings = new List<string>();
            if (parseResult.Warnings is { Count: > 0 } parseWarnings)
                warnings.AddRange(parseWarnings);

            var parsedCSharpByFullPath = parseResult.Value!.ToDictionary(
                d => Path.GetFullPath(d.AbsolutePath),
                d => d,
                StringComparer.OrdinalIgnoreCase);

            var loadResult = LoadFiles(sortedPaths, project, parsedCSharpByFullPath, warnings);
            var md = ComposeFeedMarkdown(feedName, project.ProjectName, loadResult);

            return ExtractionResult<string>.Success(
                md,
                warnings.Count > 0 ? warnings : null);
        }
        catch (Exception ex)
        {
            return ExtractionResult<string>.Failure($"Fehler bei der Feed-Generierung für {project.ProjectName}: {ex.Message}");
        }
    }

    private static string ComposeFeedMarkdown(string feedName, string projectDisplayName, LoadFilesResult loadResult)
    {
        var sessionId = Guid.NewGuid().ToString();
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");

        var sb = new StringBuilder();

        // YAML Frontmatter
        sb.AppendLine("---");
        sb.AppendLine("feed_type: source_export");
        sb.AppendLine($"project: \"{YamlDoubleQuotedEscaping.EscapeYamlDoubleQuoted(feedName)}\"");
        sb.AppendLine($"session_id: {sessionId}");
        sb.AppendLine($"generated: {timestamp}");
        sb.AppendLine("part: 1");
        sb.AppendLine("total_parts: 1");
        sb.AppendLine($"file_count: {loadResult.FileContents.Count}");
        sb.AppendLine("---");
        sb.AppendLine();

        // Header & Instruction
        sb.AppendLine($"# AI FEED: {feedName}");
        sb.AppendLine("# (Part 1 of 1)");
        sb.AppendLine();
        sb.AppendLine("## INSTRUCTION");
        sb.AppendLine($"SYSTEM-KONTEXT: Dies ist ein Snapshot eines Software-Projekts. Das Format ist Markdown mit Fencing. Dies ist Projekt: '{projectDisplayName}'. Analysiere den Code im Kontext der Architektur.");
        sb.AppendLine();

        // Manifest Tabelle
        sb.AppendLine("## MANIFEST");
        sb.AppendLine("| ID | Type | Size | Path |");
        sb.AppendLine("|---:|:---|---:|:---|");
        foreach (var m in loadResult.Manifests)
        {
            sb.AppendLine($"| [{m.Id}] | {m.Type} | {m.Size} | {m.RelativePath} |");
        }
        sb.AppendLine();
        sb.AppendLine("## CONTENT");

        // Dateiinhalte mit Dynamic Fencing anfügen
        foreach (var file in loadResult.FileContents)
        {
            sb.AppendLine($"### [{file.FileId}] {file.RelativePath}");

            int requiredBackticks = MarkdownFenceUtility.CalculateRequiredBackticks(file.Content);
            string fence = new string('`', requiredBackticks);

            sb.AppendLine($"{fence}{file.Language}");
            sb.AppendLine(file.Content);
            sb.AppendLine(fence);
        }

        return sb.ToString();
    }

    private static LoadFilesResult LoadFiles(
        IReadOnlyList<string> sortedPaths,
        ProjectDefinition project,
        Dictionary<string, ParsedCSharpDocument> parsedCSharpByFullPath,
        List<string> warnings)
    {
        var fileContents = new List<FileContent>();
        var manifests = new List<FileManifestEntry>();
        var idCounter = 1;

        foreach (var path in sortedPaths)
        {
            var fullPath = Path.GetFullPath(path);
            string content;
            long size;

            if (parsedCSharpByFullPath.TryGetValue(fullPath, out var parsed))
            {
                content = parsed.SourceText;
                size = parsed.SizeBytes;
            }
            else if (string.Equals(Path.GetExtension(fullPath), ".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            else
            {
                try
                {
                    content = File.ReadAllText(fullPath);
                    size = new FileInfo(fullPath).Length;
                }
                catch (Exception ex) when (SkippableLocalFileIoExceptions.Matches(ex))
                {
                    warnings.Add($"\"{fullPath}\" uebersprungen ({ex.GetType().Name}): {ex.Message}");
                    continue;
                }
            }

            var extension = Path.GetExtension(fullPath);
            var relativePath = Path.GetRelativePath(project.RootDirectory, fullPath);
            var (type, language) = FileTypeService.GetFileTypeAndLanguage(extension);

            fileContents.Add(new FileContent(idCounter, relativePath, content, type, language, size));
            manifests.Add(new FileManifestEntry(idCounter, type, size, relativePath));
            idCounter++;
        }

        return new LoadFilesResult(fileContents, manifests);
    }
}
