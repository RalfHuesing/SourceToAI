using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services;

namespace SourceToAI.CLI.Services.Discovery;

public class FileDiscoveryService(IDirectoryEnumerator directoryEnumerator) : IFileDiscoveryService
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
        var warnings = new List<string>();

        try
        {
            ScanDirectory(project.RootDirectory, foundFiles, settings, warnings);
            return ExtractionResult<List<string>>.Success(
                foundFiles,
                warnings.Count > 0 ? warnings : null);
        }
        catch (Exception ex)
        {
            return ExtractionResult<List<string>>.Failure($"Fehler beim Scannen von {project.ProjectName}: {ex.Message}");
        }
    }

    private void ScanDirectory(string currentDir, List<string> foundFiles, AppSettings settings, List<string> warnings)
    {
        string[] files;
        try
        {
            files = directoryEnumerator.EnumerateFiles(currentDir).ToArray();
        }
        catch (Exception ex) when (SkippableLocalFileIoExceptions.Matches(ex))
        {
            warnings.Add($"Dateien in „{currentDir}“ nicht lesbar ({ex.GetType().Name}): {ex.Message}");
            return;
        }

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (settings.IncludedExtensions.Contains(ext))
            {
                foundFiles.Add(file);
            }
        }

        string[] subDirs;
        try
        {
            subDirs = directoryEnumerator.EnumerateDirectories(currentDir).ToArray();
        }
        catch (Exception ex) when (SkippableLocalFileIoExceptions.Matches(ex))
        {
            warnings.Add($"Unterverzeichnisse von „{currentDir}“ nicht lesbar ({ex.GetType().Name}): {ex.Message}");
            return;
        }

        foreach (var dir in subDirs)
        {
            var dirName = new DirectoryInfo(dir).Name;

            if (!settings.ExcludedDirectories.Contains(dirName, StringComparer.OrdinalIgnoreCase))
            {
                ScanDirectory(dir, foundFiles, settings, warnings);
            }
        }
    }
}
