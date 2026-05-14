namespace SourceToAI.CLI.Configuration;

public class AppSettings
{
    /// <summary>
    /// Fallback-Werte entsprechen <c>appsettings.json</c>, damit der Scan ohne JSON-Datei sinnvoll arbeitet.
    /// </summary>
    public string[] ExcludedDirectories { get; set; } =
    [
        "bin", "obj", ".git", ".vs", ".idea", "node_modules"
    ];

    /// <summary>
    /// Glob-Muster relativ zum jeweiligen Projektstamm (Ordner der .csproj); siehe Microsoft.Extensions.FileSystemGlobbing.
    /// </summary>
    public string[] ExcludedPathPatterns { get; set; } = [];

    public string[] IncludedExtensions { get; set; } =
    [
        ".cs", ".sql", ".json", ".xml", ".xaml", ".yml", ".md", ".mdc", ".js", ".ts", ".css",
        ".cshtml", ".html", ".http", ".razor", ".svg", ".txt", ".csproj"
    ];
}
