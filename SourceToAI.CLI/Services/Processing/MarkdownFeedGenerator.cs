using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services;
using SourceToAI.CLI.Services.Export.AiFeed;
using SourceToAI.CLI.Services.Processing.Markdown;
using System.Text;

namespace SourceToAI.CLI.Services.Processing;

public class MarkdownFeedGenerator(ICSharpDocumentLoader csharpDocumentLoader) : IFeedGenerator
{
    public ExtractionResult<string> GenerateFeed(string solutionName, ProjectDefinition project, List<string> filePaths)
    {
        try
        {
            csharpDocumentLoader.Clear();

            var sessionId = Guid.NewGuid().ToString();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
            var feedName = $"{solutionName} ({project.ProjectName})";

            var fileContents = new List<FileContent>();
            var manifests = new List<FileManifestEntry>();

            var sortedPaths = filePaths
                .OrderByDescending(p => Path.GetExtension(p).Equals(".md", StringComparison.OrdinalIgnoreCase))
                .ThenBy(p => p)
                .ToList();

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

            // 1. Dateien einlesen und analysieren (.cs über Parse-Pipeline, übrige Extensions einmalig direkt vom Dateisystem)
            int idCounter = 1;
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
                    // Beim Parsen übersprungen — Hinweise stehen in parseResult.Warnings
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
                        warnings.Add(
                            $"\"{fullPath}\" uebersprungen ({ex.GetType().Name}): {ex.Message}");
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

            // 2. Dokument zusammenbauen
            var sb = new StringBuilder();

            // YAML Frontmatter
            sb.AppendLine("---");
            sb.AppendLine("feed_type: source_export");
            sb.AppendLine($"project: \"{YamlDoubleQuotedEscaping.EscapeYamlDoubleQuoted(feedName)}\"");
            sb.AppendLine($"session_id: {sessionId}");
            sb.AppendLine($"generated: {timestamp}");
            sb.AppendLine("part: 1");
            sb.AppendLine("total_parts: 1");
            sb.AppendLine($"file_count: {fileContents.Count}");
            sb.AppendLine("---");
            sb.AppendLine();

            // Header & Instruction
            sb.AppendLine($"# AI FEED: {feedName}");
            sb.AppendLine("# (Part 1 of 1)");
            sb.AppendLine();
            sb.AppendLine("## INSTRUCTION");
            sb.AppendLine($"SYSTEM-KONTEXT: Dies ist ein Snapshot eines Software-Projekts. Das Format ist Markdown mit Fencing. Dies ist Projekt: '{project.ProjectName}'. Analysiere den Code im Kontext der Architektur.");
            sb.AppendLine();

            // Manifest Tabelle
            sb.AppendLine("## MANIFEST");
            sb.AppendLine("| ID | Type | Size | Path |");
            sb.AppendLine("|---:|:---|---:|:---|");
            foreach (var m in manifests)
            {
                sb.AppendLine($"| [{m.Id}] | {m.Type} | {m.Size} | {m.RelativePath} |");
            }
            sb.AppendLine();
            sb.AppendLine("## CONTENT");

            // Dateiinhalte mit Dynamic Fencing anfügen
            foreach (var file in fileContents)
            {
                sb.AppendLine($"### [{file.FileId}] {file.RelativePath}");

                // Dynamic Fencing: Bestimme, wie viele Backticks wir brauchen (mindestens 4, mehr wenn im Code schon welche sind)
                int requiredBackticks = MarkdownFenceUtility.CalculateRequiredBackticks(file.Content);
                string fence = new string('`', requiredBackticks);

                sb.AppendLine($"{fence}{file.Language}");
                sb.AppendLine(file.Content);
                sb.AppendLine(fence);
            }

            return ExtractionResult<string>.Success(
                sb.ToString(),
                warnings.Count > 0 ? warnings : null);
        }
        catch (Exception ex)
        {
            return ExtractionResult<string>.Failure($"Fehler bei der Feed-Generierung für {project.ProjectName}: {ex.Message}");
        }
    }
}
