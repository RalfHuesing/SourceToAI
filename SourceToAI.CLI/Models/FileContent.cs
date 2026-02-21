namespace SourceToAI.CLI.Models;

/// <summary>
/// Repräsentiert den Inhalt und die Metadaten einer eingelesenen Quellcode-Datei.
/// </summary>
public record FileContent(
    int FileId,
    string RelativePath,
    string Content,
    string Type,
    string Language,
    string Hash,
    long SizeBytes);