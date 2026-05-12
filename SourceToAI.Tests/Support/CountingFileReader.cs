using SourceToAI.CLI.Services.IO;

namespace SourceToAI.Tests.Support;

/// <summary>
/// Zählt <see cref="IFileReader.ReadAllText"/>-Aufrufe pro normalisiertem Pfad (Tests: „einmal lesen“).
/// </summary>
public sealed class CountingFileReader(IFileReader inner) : IFileReader
{
    private readonly IFileReader _inner = inner;

    public Dictionary<string, int> ReadCounts { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string ReadAllText(string path)
    {
        var key = Path.GetFullPath(path);
        ReadCounts.TryGetValue(key, out var n);
        ReadCounts[key] = n + 1;
        return _inner.ReadAllText(key);
    }
}
