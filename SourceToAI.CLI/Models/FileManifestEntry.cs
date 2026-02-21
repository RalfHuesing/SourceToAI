namespace SourceToAI.CLI.Models;

/// <summary>
/// Repräsentiert eine Zeile in der Manifest-Tabelle der generierten Markdown-Datei.
/// </summary>
public record FileManifestEntry(
    int Id,
    string Type,
    string Hash,
    long Size,
    string RelativePath);