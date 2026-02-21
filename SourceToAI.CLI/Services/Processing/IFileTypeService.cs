namespace SourceToAI.CLI.Services.Processing;

public interface IFileTypeService
{
    /// <summary>
    /// Ermittelt basierend auf der Dateiendung den übergeordneten Typ (z.B. "Code", "Doc")
    /// und den Markdown-Language-Tag (z.B. "csharp", "xml").
    /// </summary>
    (string Type, string Language) GetFileTypeAndLanguage(string extension);
}