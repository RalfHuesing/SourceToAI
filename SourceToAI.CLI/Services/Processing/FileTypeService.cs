using System;
using System.Collections.Generic;

namespace SourceToAI.CLI.Services.Processing;

/// <summary>
/// Ermittelt aus der Dateiendung Kategorie (z. B. „Code“, „Doc“) und Markdown-Sprach-Tag — reine Heuristik, ohne I/O.
/// </summary>
public static class FileTypeService
{
    private static readonly Dictionary<string, (string Type, string Language)> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // C# & .NET
        { ".cs", ("Code", "csharp") },
        { ".xaml", ("UI", "xml") },
        { ".razor", ("UI", "razor") },
        { ".cshtml", ("UI", "html") },

        // Web
        { ".js", ("Code", "javascript") },
        { ".ts", ("Code", "typescript") },
        { ".css", ("Code", "css") },
        { ".html", ("UI", "html") },
        { ".svg", ("UI", "svg") },
        { ".http", ("Config", "http") },

        // Database
        { ".sql", ("Data", "sql") },

        // Config & Data
        { ".json", ("Config", "json") },
        { ".xml", ("Config", "xml") },
        { ".yaml", ("Config", "yaml") },
        { ".yml", ("Config", "yaml") },
        { ".ini", ("Config", "ini") },

        // Documentation
        { ".md", ("Doc", "markdown") },
        { ".mdc", ("Doc", "markdown") },
        { ".txt", ("Doc", "text") }
    };

    /// <summary>
    /// Ermittelt basierend auf der Dateiendung den übergeordneten Typ (z.B. "Code", "Doc")
    /// und den Markdown-Language-Tag (z.B. "csharp", "xml").
    /// </summary>
    public static (string Type, string Language) GetFileTypeAndLanguage(string extension)
    {
        var ext = extension.ToLowerInvariant();
        return Extensions.TryGetValue(ext, out var val) ? val : ("Unknown", "text");
    }
}
