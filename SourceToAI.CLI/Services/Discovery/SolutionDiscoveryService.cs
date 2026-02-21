using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Discovery;

public class SolutionDiscoveryService : ISolutionDiscoveryService
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

        // Fallback: Nimm den Namen des Root-Verzeichnisses
        var dirName = new DirectoryInfo(rootPath).Name;
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