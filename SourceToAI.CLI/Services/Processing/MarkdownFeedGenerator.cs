using SourceToAI.CLI.Models;
using System.Text;

namespace SourceToAI.CLI.Services.Processing;

public class MarkdownFeedGenerator(
    IFileTypeService fileTypeService,
    IHashService hashService) : IFeedGenerator
{
    public ExtractionResult<string> GenerateFeed(string solutionName, ProjectDefinition project, List<string> filePaths)
    {
        try
        {
            var sessionId = Guid.NewGuid().ToString();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
            var feedName = $"{solutionName} ({project.ProjectName})";

            var fileContents = new List<FileContent>();
            var manifests = new List<FileManifestEntry>();

            var sortedPaths = filePaths
                .OrderByDescending(p => Path.GetExtension(p).Equals(".md", StringComparison.OrdinalIgnoreCase))
                .ThenBy(p => p)
                .ToList();

            // 1. Dateien einlesen und analysieren
            int idCounter = 1;
            foreach (var path in sortedPaths)
            {
                var content = File.ReadAllText(path);
                var extension = Path.GetExtension(path);
                var size = new FileInfo(path).Length;
                var relativePath = Path.GetRelativePath(project.RootDirectory, path);

                var (type, language) = fileTypeService.GetFileTypeAndLanguage(extension);
                var hash = hashService.ComputeShortHash(content);

                fileContents.Add(new FileContent(idCounter, relativePath, content, type, language, hash, size));
                manifests.Add(new FileManifestEntry(idCounter, type, hash, size, relativePath));

                idCounter++;
            }

            // 2. Dokument zusammenbauen
            var sb = new StringBuilder();

            // YAML Frontmatter
            sb.AppendLine("---");
            sb.AppendLine("feed_type: source_export");
            sb.AppendLine($"project: {feedName}");
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
            sb.AppendLine("---");
            sb.AppendLine();

            // Manifest Tabelle
            sb.AppendLine("## MANIFEST");
            sb.AppendLine("| ID | Type | Hash | Size | Path |");
            sb.AppendLine("|---:|:---|:---|---:|:---|");
            foreach (var m in manifests)
            {
                sb.AppendLine($"| [{m.Id}](#{m.Id}) | {m.Type} | {m.Hash} | {m.Size} | {m.RelativePath} |");
            }
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## CONTENT");
            sb.AppendLine();

            // Dateiinhalte mit Dynamic Fencing anfügen
            foreach (var file in fileContents)
            {
                sb.AppendLine("---");
                sb.AppendLine($"### [{file.FileId}] {file.RelativePath}");

                // Dynamic Fencing: Bestimme, wie viele Backticks wir brauchen (mindestens 4, mehr wenn im Code schon welche sind)
                int requiredBackticks = CalculateRequiredBackticks(file.Content);
                string fence = new string('`', requiredBackticks);

                sb.AppendLine($"{fence}{file.Language}");
                sb.AppendLine(file.Content);
                sb.AppendLine(fence);
                sb.AppendLine();
            }

            return ExtractionResult<string>.Success(sb.ToString());
        }
        catch (Exception ex)
        {
            return ExtractionResult<string>.Failure($"Fehler bei der Feed-Generierung für {project.ProjectName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Analysiert den Code nach vorhandenen Backticks und gibt eine sichere Anzahl für den Block zurück.
    /// </summary>
    private int CalculateRequiredBackticks(string content)
    {
        int maxConsecutiveBackticks = 0;
        int currentConsecutive = 0;

        foreach (char c in content)
        {
            if (c == '`')
            {
                currentConsecutive++;
                if (currentConsecutive > maxConsecutiveBackticks)
                    maxConsecutiveBackticks = currentConsecutive;
            }
            else
            {
                currentConsecutive = 0;
            }
        }

        // Mindestens 4 Backticks, ansonsten (Maximal gefundene + 1)
        return Math.Max(4, maxConsecutiveBackticks + 1);
    }
}
