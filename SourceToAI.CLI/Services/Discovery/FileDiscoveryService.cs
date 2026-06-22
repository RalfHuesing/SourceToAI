using System.Linq;
using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services;

namespace SourceToAI.CLI.Services.Discovery;

public sealed class FileDiscoveryService(IDirectoryEnumerator directoryEnumerator) : IFileDiscoveryService
{
    /// <summary>
    /// Direkt unter der Solution-Wurzel — gleiche Sonderfälle wie <see cref="FindSolutionDocs"/> (kein rekursiver „Unmapped“-Export dieser Bäume).
    /// </summary>
    private static readonly HashSet<string> UnmappedSkippedTopLevelDirectoryNames =
        new(StringComparer.OrdinalIgnoreCase) { ".cursor", ".github", "Docs" };

    private readonly struct ScanContext(
        string scanRoot,
        ProjectPathExcludeSpec? scanPathExclude,
        ProjectPathExcludeSpec? solutionPathExclude,
        HashSet<string> includedExtensions,
        HashSet<string> excludedDirectories,
        List<string> foundFiles,
        List<string> warnings)
    {
        public string ScanRoot { get; } = scanRoot;
        public ProjectPathExcludeSpec? ScanPathExclude { get; } = scanPathExclude;
        public ProjectPathExcludeSpec? SolutionPathExclude { get; } = solutionPathExclude;
        public HashSet<string> IncludedExtensions { get; } = includedExtensions;
        public HashSet<string> ExcludedDirectories { get; } = excludedDirectories;
        public List<string> FoundFiles { get; } = foundFiles;
        public List<string> Warnings { get; } = warnings;
    }

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

    public ExtractionResult<List<string>> FindFilesForProject(
        ProjectDefinition project,
        string solutionRoot,
        AppSettings settings)
    {
        if (!Directory.Exists(project.RootDirectory))
            return ExtractionResult<List<string>>.Failure($"Projektverzeichnis {project.RootDirectory} existiert nicht.");

        var foundFiles = new List<string>();
        var warnings = new List<string>();

        try
        {
            var included = new HashSet<string>(settings.IncludedExtensions, StringComparer.OrdinalIgnoreCase);
            var excluded = new HashSet<string>(settings.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);
            var projectPathExclude = ProjectPathExcludeFilter.TryCreate(
                settings.ExcludedPathPatterns,
                project.RootDirectory);
            var solutionPathExclude = ProjectPathExcludeFilter.TryCreate(
                settings.ExcludedPathPatterns,
                solutionRoot);

            var context = new ScanContext(
                project.RootDirectory,
                projectPathExclude,
                solutionPathExclude,
                included,
                excluded,
                foundFiles,
                warnings);

            ScanDirectory(project.RootDirectory, context);

            return ExtractionResult<List<string>>.Success(
                foundFiles,
                warnings.Count > 0 ? warnings : null);
        }
        catch (Exception ex)
        {
            return ExtractionResult<List<string>>.Failure($"Fehler beim Scannen von {project.ProjectName}: {ex.Message}");
        }
    }

    private static (HashSet<string> ProjectRoots, HashSet<string> Included, HashSet<string> Excluded, ProjectPathExcludeSpec? SolutionFilter)
        PrepareUnmappedScanSets(string rootFull, IReadOnlyList<ProjectDefinition> projects, AppSettings settings)
    {
        var projectRoots = new HashSet<string>(
            projects
                .Where(p => !string.IsNullOrEmpty(p.RootDirectory))
                .Select(p => Path.GetFullPath(p.RootDirectory)),
            StringComparer.OrdinalIgnoreCase);

        var included = new HashSet<string>(settings.IncludedExtensions, StringComparer.OrdinalIgnoreCase);
        var excluded = new HashSet<string>(settings.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);
        var solutionPathExclude = ProjectPathExcludeFilter.TryCreate(settings.ExcludedPathPatterns, rootFull);

        return (projectRoots, included, excluded, solutionPathExclude);
    }

    private IOrderedEnumerable<(string Path, string Name)> GetUnmappedDirectories(
        string rootFull,
        HashSet<string> projectRoots,
        HashSet<string> excluded,
        ProjectPathExcludeSpec? solutionPathExclude)
    {
        string[] directSubDirs;
        try
        {
            directSubDirs = directoryEnumerator.EnumerateDirectories(rootFull).ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unmapped-Verzeichnisse unter \"{rootFull}\" nicht lesbar: {ex.Message}", ex);
        }

        return directSubDirs
            .Select(d => (Path: Path.GetFullPath(d), Name: new DirectoryInfo(d).Name))
            .Where(x => !excluded.Contains(x.Name)
                        && !UnmappedSkippedTopLevelDirectoryNames.Contains(x.Name)
                        && !projectRoots.Contains(x.Path)
                        && !ProjectPathExcludeFilter.IsDirectoryExcluded(solutionPathExclude, x.Path))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    public ExtractionResult<List<(string DirectoryName, List<string> AbsolutePaths)>> FindUnmappedDirectories(
        string rootPath,
        IReadOnlyList<ProjectDefinition> projects,
        AppSettings settings)
    {
        var mergedWarnings = new List<string>();

        try
        {
            var rootFull = Path.GetFullPath(rootPath);
            if (!Directory.Exists(rootFull))
            {
                return ExtractionResult<List<(string DirectoryName, List<string> AbsolutePaths)>>.Failure(
                    $"Solution-Wurzel \"{rootPath}\" existiert nicht.");
            }

            var (projectRoots, included, excluded, solutionPathExclude) = PrepareUnmappedScanSets(rootFull, projects, settings);

            IEnumerable<(string Path, string Name)> unmappedDirs;
            try
            {
                unmappedDirs = GetUnmappedDirectories(rootFull, projectRoots, excluded, solutionPathExclude);
            }
            catch (Exception ex)
            {
                return ExtractionResult<List<(string DirectoryName, List<string> AbsolutePaths)>>.Failure(ex.Message);
            }

            var results = new List<(string DirectoryName, List<string> AbsolutePaths)>();
            foreach (var (dirFull, dirName) in unmappedDirs)
            {
                var unmappedPathExclude = ProjectPathExcludeFilter.TryCreate(settings.ExcludedPathPatterns, dirFull);
                var foundFiles = new List<string>();
                var context = new ScanContext(
                    dirFull,
                    unmappedPathExclude,
                    solutionPathExclude,
                    included,
                    excluded,
                    foundFiles,
                    mergedWarnings);

                ScanDirectory(dirFull, context);

                if (foundFiles.Count > 0)
                {
                    results.Add((dirName, foundFiles));
                }
            }

            return ExtractionResult<List<(string DirectoryName, List<string> AbsolutePaths)>>.Success(
                results,
                mergedWarnings.Count > 0 ? mergedWarnings : null);
        }
        catch (Exception ex)
        {
            return ExtractionResult<List<(string DirectoryName, List<string> AbsolutePaths)>>.Failure(
                $"Fehler bei Unmapped-Verzeichnis-Erkennung: {ex.Message}");
        }
    }

    private void ScanDirectory(string currentDir, ScanContext context)
    {
        string[] files;
        try
        {
            files = directoryEnumerator.EnumerateFiles(currentDir).ToArray();
        }
        catch (Exception ex) when (SkippableLocalFileIoExceptions.Matches(ex))
        {
            context.Warnings.Add($"Dateien in \"{currentDir}\" nicht lesbar ({ex.GetType().Name}): {ex.Message}");
            return;
        }

        foreach (var file in files)
        {
            if (!context.IncludedExtensions.Contains(Path.GetExtension(file)))
                continue;

            if (ProjectPathExcludeFilter.IsPathExcludedByAny(context.ScanPathExclude, context.SolutionPathExclude, file, isDirectory: false))
                continue;

            context.FoundFiles.Add(file);
        }

        string[] subDirs;
        try
        {
            subDirs = directoryEnumerator.EnumerateDirectories(currentDir).ToArray();
        }
        catch (Exception ex) when (SkippableLocalFileIoExceptions.Matches(ex))
        {
            context.Warnings.Add($"Unterverzeichnisse von \"{currentDir}\" nicht lesbar ({ex.GetType().Name}): {ex.Message}");
            return;
        }

        foreach (var dir in subDirs)
        {
            var dirName = new DirectoryInfo(dir).Name;

            if (context.ExcludedDirectories.Contains(dirName))
                continue;

            if (ProjectPathExcludeFilter.IsPathExcludedByAny(context.ScanPathExclude, context.SolutionPathExclude, dir, isDirectory: true))
                continue;

            ScanDirectory(dir, context);
        }
    }
}
