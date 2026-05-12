namespace SourceToAI.CLI.Services.Processing;

/// <summary>
/// Kontext pro Datei für <see cref="IViewGenerator"/> — kein Dateizugriff, nur Metadaten.
/// </summary>
public sealed class ViewGeneratorContext
{
    public ViewGeneratorContext(string relativePath)
    {
        RelativePath = relativePath;
    }

    /// <summary>Projektrelativer Pfad (wie in <see cref="Models.ParsedCSharpDocument"/>).</summary>
    public string RelativePath { get; }
}
