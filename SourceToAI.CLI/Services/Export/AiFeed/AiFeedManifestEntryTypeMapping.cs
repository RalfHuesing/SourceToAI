namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Abbildung der heuristischen Dateityp-Strings (z. B. aus <see cref="SourceToAI.CLI.Services.Processing.IFileTypeService"/>) auf die Konzept-Manifest-Typen.
/// </summary>
public static class AiFeedManifestEntryTypeMapping
{
    /// <summary>
    /// <c>Doc</c> aus dem File-Type-Service wird 1:1 übernommen; alle anderen Kategorien werden als <see cref="AiFeedManifestEntryType.Code"/> geführt
    /// (Konzept-Beispiel nutzt primär Code/Dokumentation).
    /// </summary>
    public static AiFeedManifestEntryType FromFileTypeCategory(string fileTypeCategory)
    {
        ArgumentNullException.ThrowIfNull(fileTypeCategory);
        return fileTypeCategory.Equals("Doc", StringComparison.Ordinal)
            ? AiFeedManifestEntryType.Doc
            : AiFeedManifestEntryType.Code;
    }
}
