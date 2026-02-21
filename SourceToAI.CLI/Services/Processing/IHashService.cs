namespace SourceToAI.CLI.Services.Processing;

public interface IHashService
{
    /// <summary>
    /// Berechnet einen kurzen (8 Zeichen) MD5-Hash für den übergebenen Datei-Inhalt.
    /// </summary>
    string ComputeShortHash(string content);
}