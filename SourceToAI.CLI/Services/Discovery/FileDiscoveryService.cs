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
            var included = new HashSet<string>(settings.IncludedExtensions, StringComparer.OrdinalIgnoreCase);
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
                    .Where(f => included.Contains(Path.GetExtension(f)));

                foundFiles.AddRange(ruleFiles);
            }
            // 3. .github/workflows Verzeichnis prüfen
            var githubDir = Path.Combine(rootPath, ".github", "workflows");
            if (Directory.Exists(githubDir))
            {
                var gitHubFiles = Directory.GetFiles(githubDir, "*.*", SearchOption.AllDirectories)
                    .Where(f => included.Contains(Path.GetExtension(f)));

                foundFiles.AddRange(gitHubFiles);
            }

            // 4. Docs/ — nur oberste Ebene: kein rekursiver Scan (bewusst, um große Unterbäume
            //    nicht in den virtuellen .Docs-Feed zu ziehen und das Verhalten vorhersehbar zu halten).
            var docsDir = Path.Combine(rootPath, "Docs");
            if (Directory.Exists(docsDir))
            {
                var docsFiles = Directory.GetFiles(docsDir, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f =>
                    {
                        var ext = Path.GetExtension(f);
                        return ext.Equals(".md", StringComparison.OrdinalIgnoreCase)
                            || ext.Equals(".mdc", StringComparison.OrdinalIgnoreCase);
                    })
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                foundFiles.AddRange(docsFiles);
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
            var included = new HashSet<string>(settings.IncludedExtensions, StringComparer.OrdinalIgnoreCase);
            var excluded = new HashSet<string>(settings.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);
            ScanDirectory(project.RootDirectory, foundFiles, included, excluded, warnings);
            return ExtractionResult<List<string>>.Success(
                foundFiles,
                warnings.Count > 0 ? warnings : null);
        }
        catch (Exception ex)
        {
            return ExtractionResult<List<string>>.Failure($"Fehler beim Scannen von {project.ProjectName}: {ex.Message}");
        }
    }

    private void ScanDirectory(
        string currentDir,
        List<string> foundFiles,
        HashSet<string> includedExtensions,
        HashSet<string> excludedDirectories,
        List<string> warnings)
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
            if (includedExtensions.Contains(Path.GetExtension(file)))
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

            if (!excludedDirectories.Contains(dirName))
            {
                ScanDirectory(dir, foundFiles, includedExtensions, excludedDirectories, warnings);
            }
        }
    }
}
