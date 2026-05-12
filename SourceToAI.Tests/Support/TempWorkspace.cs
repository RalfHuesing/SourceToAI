using System.Text;

namespace SourceToAI.Tests.Support;

/// <summary>
/// Eindeutiges temporäres Arbeitsverzeichnis für Dateisystem-Tests; räumt bei Dispose rekursiv auf.
/// </summary>
public sealed class TempWorkspace : IDisposable
{
    public string Root { get; }

    public TempWorkspace()
    {
        Root = Path.Combine(Path.GetTempPath(), "SourceToAI.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string WriteFile(string relativePath, string content, Encoding? encoding = null)
    {
        var fullPath = Path.Combine(Root, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(fullPath, content, encoding ?? Encoding.UTF8);
        return fullPath;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
        catch
        {
            // Aufräumen ist best effort; fehlgeschlagene Tests sollen nicht an IO hängen bleiben.
        }
    }
}
