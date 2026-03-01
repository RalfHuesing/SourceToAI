using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Discovery;

public class FileDiscoveryService : IFileDiscoveryService
{
    public ExtractionResult<List<string>> FindSolutionDocs(string rootPath, AppSettings settings)
    {
        var foundFiles = new List<string>();

        try
        {
            // 1. Root README.md prüfen
            var rootReadme = Path.Combine(rootPath, "README.md");
            if (File.Exists(rootReadme))
            {
                foundFiles.Add(rootReadme);
            }

            // 2. .cursor/rules Verzeichnis prüfen
            var cursorRulesDir = Path.Combine(rootPath, ".cursor", "rules");
            if (Directory.Exists(cursorRulesDir))
            {
                var ruleFiles = Directory.GetFiles(cursorRulesDir, "*.*", SearchOption.AllDirectories)
                    .Where(f => settings.IncludedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

                foundFiles.AddRange(ruleFiles);
            }
            // 3. .cursor/rules Verzeichnis prüfen
            var githubDir = Path.Combine(rootPath, ".github", "workflows");
            if (Directory.Exists(githubDir))
            {
                var gitHubFiles = Directory.GetFiles(githubDir, "*.*", SearchOption.AllDirectories)
                    .Where(f => settings.IncludedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

                foundFiles.AddRange(gitHubFiles);
            }
            return ExtractionResult<List<string>>.Success(foundFiles);
        }
        catch (Exception ex)
        {
            return ExtractionResult<List<string>>.Failure($"Fehler beim Suchen der Solution-Docs: {ex.Message}");
        }
    }

    public ExtractionResult<List<string>> FindFilesForProject(ProjectDefinition project, AppSettings settings)
    {
        if (!Directory.Exists(project.RootDirectory))
            return ExtractionResult<List<string>>.Failure($"Projektverzeichnis {project.RootDirectory} existiert nicht.");

        var foundFiles = new List<string>();

        try
        {
            ScanDirectory(project.RootDirectory, foundFiles, settings);
            return ExtractionResult<List<string>>.Success(foundFiles);
        }
        catch (Exception ex)
        {
            return ExtractionResult<List<string>>.Failure($"Fehler beim Scannen von {project.ProjectName}: {ex.Message}");
        }
    }

    private void ScanDirectory(string currentDir, List<string> foundFiles, AppSettings settings)
    {
        // 1. Dateien im aktuellen Verzeichnis prüfen
        foreach (var file in Directory.GetFiles(currentDir))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (settings.IncludedExtensions.Contains(ext))
            {
                foundFiles.Add(file);
            }
        }

        // 2. Unterverzeichnisse prüfen (und ggf. ignorieren)
        foreach (var dir in Directory.GetDirectories(currentDir))
        {
            var dirName = new DirectoryInfo(dir).Name;

            // Wenn der Ordner nicht auf der Blacklist steht, rekursiv weiter abtauchen
            if (!settings.ExcludedDirectories.Contains(dirName, StringComparer.OrdinalIgnoreCase))
            {
                ScanDirectory(dir, foundFiles, settings);
            }
        }
    }
}
