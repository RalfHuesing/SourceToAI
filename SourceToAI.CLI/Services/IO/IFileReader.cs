namespace SourceToAI.CLI.Services.IO;

/// <summary>
/// Abstraktion für Datei-Lesezugriff (Tests können Aufrufe zählen oder stubben).
/// </summary>
public interface IFileReader
{
    string ReadAllText(string path);
}
