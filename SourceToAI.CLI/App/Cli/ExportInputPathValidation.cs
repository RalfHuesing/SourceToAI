namespace SourceToAI.CLI.App.Cli;

/// <summary>
/// Frühvalidierung der CLI-Eingabepfade (Verzeichnis oder .NET-Assembly), bevor DI und Orchestrierung starten.
/// </summary>
internal static class ExportInputPathValidation
{
    /// <summary>
    /// Liefert bei ungültigem Pfad eine deutschsprachige Fehlermeldung, sonst <c>null</c>.
    /// </summary>
    internal static string? GetValidationError(string path)
    {
        if (Directory.Exists(path))
            return null;

        if (File.Exists(path))
        {
            var ext = Path.GetExtension(path);
            if (ext.Equals(".dll", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".exe", StringComparison.OrdinalIgnoreCase))
                return null;

            return
                $"Ungültiger Eingabepfad (nicht unterstützter Dateityp): \"{path}\". Erlaubt sind Verzeichnisse oder .NET-Assemblies (.dll, .exe).";
        }

        return
            $"Ungültiger Eingabepfad (nicht vorhanden): \"{path}\". Erwartet wird ein existierendes Verzeichnis oder eine .dll/.exe-Datei.";
    }
}
