namespace SourceToAI.CLI.Services.Export;

/// <summary>
/// Erzeugt die beschreibende <c>readme.md</c> im Export-Wurzelverzeichnis (Konzept Abschnitt 2 / Task 08).
/// </summary>
public interface IMultiViewReadmeMarkdownGenerator
{
    string Generate(string repositoryRootFolderName, DateTimeOffset generatedAtUtc);
}
