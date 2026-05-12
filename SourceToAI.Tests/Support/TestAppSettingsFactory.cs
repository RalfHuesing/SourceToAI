using SourceToAI.CLI.Configuration;

namespace SourceToAI.Tests.Support;

/// <summary>
/// Typische <see cref="AppSettings"/> wie in appsettings.json, damit Tests nicht Arrays duplizieren.
/// </summary>
public static class TestAppSettingsFactory
{
    public static AppSettings Default() => new()
    {
        ExcludedDirectories =
        [
            "bin", "obj", ".git", ".vs", ".idea", "node_modules"
        ],
        IncludedExtensions =
        [
            ".cs", ".sql", ".json", ".xml", ".xaml", ".yml", ".md", ".mdc", ".js", ".ts", ".css", ".csproj"
        ]
    };
}
