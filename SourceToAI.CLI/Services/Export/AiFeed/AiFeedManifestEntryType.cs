namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Klassifikation einer Manifestzeile gemäß Konzept (Spalte „Type“: z. B. Code vs. Dokumentation).
/// </summary>
public enum AiFeedManifestEntryType
{
    Code,
    Doc
}
