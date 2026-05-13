using SourceToAI.CLI.Configuration;

namespace SourceToAI.Tests.Support;

/// <summary>
/// Einheitliche Test-<see cref="AppSettings"/> ohne Duplikat der Produktions-Defaults in <see cref="AppSettings"/>.
/// </summary>
public static class TestAppSettingsFactory
{
    public static AppSettings Default() => new();
}
