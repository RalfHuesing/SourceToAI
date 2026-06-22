using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Discovery;

public sealed class SolutionDiscoveryService : ISolutionDiscoveryService
{
    public ExtractionResult<string> GetSolutionName(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            return ExtractionResult<string>.Failure($"Das Verzeichnis {rootPath} existiert nicht.");

        var slnFiles = Directory.GetFiles(rootPath, "*.sln", SearchOption.TopDirectoryOnly);

        if (slnFiles.Length > 0)
        {
            // Nimm den Namen der ersten .sln Datei ohne Endung
            return ExtractionResult<string>.Success(Path.GetFileNameWithoutExtension(slnFiles[0]));
        }

        // Fallback: Root-Verzeichnisname. Unter …/{AssemblyName}/decompile (WholeProjectDecompiler) den
        // übergeordneten Namen nutzen, damit Export-Pfade und Readme zum Assembly-Konzept passen.
        var normalizedRoot = Path.TrimEndingDirectorySeparator(rootPath);
        var dirInfo = new DirectoryInfo(normalizedRoot);
        var dirName = dirInfo.Name;

        if (string.Equals(dirName, "decompile", StringComparison.OrdinalIgnoreCase))
        {
            // Ohne nutzbaren Parent (z. B. Dateisystem-Wurzel): weiterhin „decompile“.
            var parent = dirInfo.Parent;
            var displayName = parent != null ? parent.Name : dirName;
            return ExtractionResult<string>.Success(string.IsNullOrEmpty(displayName) ? dirName : displayName);
        }

        return ExtractionResult<string>.Success(dirName);
    }

    public ExtractionResult<List<ProjectDefinition>> FindProjects(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            return ExtractionResult<List<ProjectDefinition>>.Failure($"Das Verzeichnis {rootPath} existiert nicht.");

        var csprojFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);

        if (csprojFiles.Length == 0)
            return ExtractionResult<List<ProjectDefinition>>.Failure($"Keine .csproj Dateien in {rootPath} gefunden.");

        var projects = csprojFiles.Select(file => new ProjectDefinition(
            ProjectName: Path.GetFileNameWithoutExtension(file),
            ProjectFilePath: file
        )).ToList();

        return ExtractionResult<List<ProjectDefinition>>.Success(projects);
    }
}