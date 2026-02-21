using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Discovery;

public interface IFileDiscoveryService
{
    /// <summary>
    /// Sucht alle relevanten Dateien für ein Projekt basierend auf den AppSettings-Regeln.
    /// </summary>
    ExtractionResult<List<string>> FindFilesForProject(ProjectDefinition project, AppSettings settings);

    ExtractionResult<List<string>> FindSolutionDocs(string rootPath, AppSettings settings);
}