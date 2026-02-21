namespace SourceToAI.CLI.Services.Processing;

public class FileTypeService : IFileTypeService
{
    public (string Type, string Language) GetFileTypeAndLanguage(string extension)
    {
        var ext = extension.ToLowerInvariant();

        return ext switch
        {
            // C# & .NET
            ".cs" => ("Code", "csharp"),
            ".xaml" => ("UI", "xml"),
            ".razor" => ("UI", "razor"),
            ".cshtml" => ("UI", "html"),

            // Web
            ".js" => ("Code", "javascript"),
            ".ts" => ("Code", "typescript"),
            ".css" => ("Code", "css"),
            ".html" => ("UI", "html"),

            // Database
            ".sql" => ("Data", "sql"),

            // Config & Data
            ".json" => ("Config", "json"),
            ".xml" => ("Config", "xml"),
            ".yaml" or ".yml" => ("Config", "yaml"),
            ".ini" => ("Config", "ini"),

            // Documentation
            ".md" or ".mdc" => ("Doc", "markdown"),
            ".txt" => ("Doc", "text"),

            // Fallback
            _ => ("Unknown", "text")
        };
    }
}