namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Manifest-Spalte „Path“: relativ zum Projektroot. Für stabile Darstellung im Markdown gemäß Konzept-Beispiel
/// wird durchgängig <c>\</c> als Verzeichnistrenner ausgegeben (unabhängig vom OS der Generierung).
/// </summary>
public static class AiFeedManifestPath
{
    public static string NormalizeForManifestTable(string relativePathFromProjectRoot)
    {
        ArgumentNullException.ThrowIfNull(relativePathFromProjectRoot);
        return relativePathFromProjectRoot.Replace('/', '\\');
    }
}
