namespace SourceToAI.CLI.Services.Discovery;

/// <summary>
/// Abstrahiert Verzeichnisauflistung für <see cref="FileDiscoveryService"/> (Tests können I/O-Fehler simulieren).
/// </summary>
public interface IDirectoryEnumerator
{
    IEnumerable<string> EnumerateFiles(string directoryPath);

    IEnumerable<string> EnumerateDirectories(string directoryPath);
}
