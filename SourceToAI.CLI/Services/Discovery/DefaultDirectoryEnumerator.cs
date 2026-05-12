namespace SourceToAI.CLI.Services.Discovery;

public sealed class DefaultDirectoryEnumerator : IDirectoryEnumerator
{
    public IEnumerable<string> EnumerateFiles(string directoryPath) =>
        Directory.GetFiles(directoryPath);

    public IEnumerable<string> EnumerateDirectories(string directoryPath) =>
        Directory.GetDirectories(directoryPath);
}
