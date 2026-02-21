using SourceToAI.CLI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceToAI.CLI.Services.Discovery;

public interface ISolutionDiscoveryService
{
    /// <summary>
    /// Ermittelt den Namen der Solution basierend auf der .sln Datei im Root-Verzeichnis.
    /// Fällt auf den Ordnernamen zurück, falls keine .sln gefunden wird.
    /// </summary>
    ExtractionResult<string> GetSolutionName(string rootPath);

    /// <summary>
    /// Sucht rekursiv nach allen .csproj Dateien im angegebenen Verzeichnis.
    /// </summary>
    ExtractionResult<List<ProjectDefinition>> FindProjects(string rootPath);
}