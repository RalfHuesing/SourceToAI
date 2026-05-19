using SourceToAI.CLI.App.Exceptions;

namespace SourceToAI.CLI.App.Cli;

/// <summary>
/// Löst in CLI-Argumenten enthaltene Dateisystem-Platzhalter (<c>*</c>, <c>?</c>) im letzten Pfadsegment auf
/// (wie von <see cref="Directory.GetFiles(string, string)"/> unterstützt). CMD/PowerShell expandieren diese nicht.
/// </summary>
internal static class InputPathResolver
{
    /// <summary>
    /// Expandiert Eingabepfade mit Wildcards zu konkreten Datei- und Verzeichnispfaden.
    /// </summary>
    /// <exception cref="SourceToAiValidationException">Keine Treffer, fehlendes Basisverzeichnis oder ungültiges Muster.</exception>
    internal static IReadOnlyList<string> Resolve(IEnumerable<string> rawPaths)
    {
        var result = new List<string>();
        foreach (var raw in rawPaths)
        {
            if (!ContainsWildcards(raw))
            {
                result.Add(raw);
                continue;
            }

            var directory = Path.GetDirectoryName(raw);
            if (string.IsNullOrEmpty(directory))
                directory = ".";

            var pattern = Path.GetFileName(raw);
            if (string.IsNullOrEmpty(pattern))
            {
                throw new SourceToAiValidationException(
                    $"Ungueltiger Platzhalter-Pfad (kein Dateiname/Muster): \"{raw}\".");
            }

            if (!Directory.Exists(directory))
            {
                throw new SourceToAiValidationException(
                    $"Platzhalter-Pfad: Basisverzeichnis existiert nicht: \"{directory}\" (Eingabe: \"{raw}\").");
            }

            var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.GetFiles(directory, pattern))
                matches.Add(Path.GetFullPath(file));
            foreach (var subDir in Directory.GetDirectories(directory, pattern))
                matches.Add(Path.GetFullPath(subDir));

            if (matches.Count == 0)
            {
                throw new SourceToAiValidationException(
                    $"Keine Treffer für Platzhalter-Pfad: \"{raw}\".");
            }

            var sorted = matches.ToList();
            sorted.Sort(StringComparer.OrdinalIgnoreCase);
            result.AddRange(sorted);
        }

        return result;
    }

    private static bool ContainsWildcards(string path) =>
        path.Contains('*', StringComparison.Ordinal) || path.Contains('?', StringComparison.Ordinal);
}
