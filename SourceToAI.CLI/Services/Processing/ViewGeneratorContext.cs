namespace SourceToAI.CLI.Services.Processing;

/// <summary>
/// Kontext pro Datei für <see cref="IViewGenerator"/> — kein Dateizugriff, nur Metadaten.
/// </summary>
public sealed class ViewGeneratorContext
{
    public ViewGeneratorContext(string relativePath, string? originalSourceText = null)
    {
        RelativePath = relativePath;
        OriginalSourceText = originalSourceText;
    }

    /// <summary>Projektrelativer Pfad (wie in <see cref="Models.ParsedCSharpDocument"/>).</summary>
    public string RelativePath { get; }

    /// <summary>
    /// Optional: Originaltext aus dem Loader für die Complete-View (1:1 ohne <c>ToFullString()</c>-Normalisierung).
    /// </summary>
    public string? OriginalSourceText { get; }
}
