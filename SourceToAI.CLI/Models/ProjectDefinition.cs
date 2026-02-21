namespace SourceToAI.CLI.Models;

/// <summary>
/// Repräsentiert ein gefundenes C# Projekt innerhalb der Solution.
/// </summary>
public record ProjectDefinition(string ProjectName, string ProjectFilePath)
{
    /// <summary>
    /// Das Verzeichnis, in dem die .csproj Datei liegt.
    /// </summary>
    public string RootDirectory => Path.GetDirectoryName(ProjectFilePath) ?? string.Empty;
}