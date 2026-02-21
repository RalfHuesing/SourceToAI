using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Processing;

public interface IFeedGenerator
{
    /// <summary>
    /// Generiert das vollständige SanMarkdownFeed Dokument für ein einzelnes Projekt.
    /// </summary>
    ExtractionResult<string> GenerateFeed(string solutionName, ProjectDefinition project, List<string> filePaths);
}