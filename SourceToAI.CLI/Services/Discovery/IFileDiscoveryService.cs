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

    /// <summary>
    /// Findet direkte Unterverzeichnisse der Solution-Wurzel ohne zugehöriges C#-Projekt (<c>.csproj</c>),
    /// scannt sie nach exportierbaren Dateien (wie <see cref="FindFilesForProject"/>) und liefert
    /// nur Einträge mit mindestens einer Datei.
    /// </summary>
    ExtractionResult<List<(string DirectoryName, List<string> AbsolutePaths)>> FindUnmappedDirectories(
        string rootPath,
        IReadOnlyList<ProjectDefinition> projects,
        AppSettings settings);
}