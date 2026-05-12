using System.Security;

namespace SourceToAI.CLI.Services;

/// <summary>
/// Gemeinsamer Filter für nicht-fatale lokale Dateisystem-Fehler (Verzeichnisscan und Dateieinlesen).
/// </summary>
public static class SkippableLocalFileIoExceptions
{
    public static bool Matches(Exception ex) =>
        ex is UnauthorizedAccessException or SecurityException or IOException;
}
