namespace SourceToAI.CLI.Services.Export;

/// <summary>
/// Zielpfade für den Multi-View-Export laut <c>Konzept.md</c> Abschnitt 2 — alles direkt unter
/// <c>{exportPath}/{solutionName}</c> (kein zusätzlicher Zwischenordner).
/// </summary>
/// <remarks>
/// <para><b>View-Key → Ausgabeordner</b> (Ordnername relativ zur Solution-Exportwurzel):</para>
/// <list type="table">
/// <listheader><term>View-Key (<see cref="SourceToAI.CLI.Services.Processing.Markdown.IMarkdownProjectViewBuilder.ViewKey"/>)</term><description>Unterordner</description></listheader>
/// <item><term><c>complete</c></term><description><see cref="CompleteFolderName"/></description></item>
/// <item><term><c>signatures-only</c></term><description><see cref="SignaturesOnlyFolderName"/></description></item>
/// <item><term><c>public-only</c></term><description><see cref="PublicOnlyFolderName"/></description></item>
/// <item><term><c>dto-only</c></term><description><see cref="DtoOnlyFolderName"/></description></item>
/// </list>
/// <para>
/// Dateiname je View und Projekt: <c>{SolutionAnzeigename}.{ProjektAnzeigename}.md</c>.
/// Der Solution-Anzeigename entspricht dem Rückgabewert von
/// <see cref="SourceToAI.CLI.Services.Discovery.ISolutionDiscoveryService.GetSolutionName"/> (erste <c>.sln</c> ohne Endung oder Name des Wurzelverzeichnisses)
/// und ist identisch zum letzten Pfadsegment von <see cref="GetSolutionExportRoot"/>.
/// Der Projekt-Anzeigename ist <see cref="SourceToAI.CLI.Models.ProjectDefinition.ProjectName"/> (Dateiname der <c>.csproj</c> ohne Endung),
/// bzw. <c>.Docs</c> für das virtuelle Solution-Dokumentationsprojekt in der Sicht <c>complete</c>.
/// </para>
/// </remarks>
public static class MultiViewExportPaths
{
    public const string CompleteFolderName = "complete";

    public const string SignaturesOnlyFolderName = "signatures-only";

    public const string PublicOnlyFolderName = "public-only";

    public const string DtoOnlyFolderName = "dto-only";

    /// <summary>
    /// Dateiname der Sicherheits-Markerdatei unter der Solution-Exportwurzel (von SourceToAI angelegt).
    /// </summary>
    public const string SafetyMarkerFileName = ".sta-marker";

    private static readonly HashSet<string> ReservedWindowsBaseNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// Wurzelverzeichnis für <c>readme.md</c>, <c>dependency-graph.md</c> und alle View-Unterordner.
    /// Absolut: <c>Path.Combine(exportPath, solutionName)</c>.
    /// </summary>
    /// <remarks>
    /// Vor jedem Lauf vom Orchestrator vollständig leeren/neu anlegen, damit keine veralteten Dateien bleiben —
    /// aber nur, wenn bereits eine <see cref="SafetyMarkerFileName"/>-Datei existiert (von einem früheren Lauf);
    /// sonst bricht die CLI zur Vermeidung von Datenverlust ab.
    /// </remarks>
    public static string GetSolutionExportRoot(string exportPath, string solutionName) =>
        Path.Combine(exportPath, solutionName);

    /// <summary>
    /// Ordnername unter der Solution-Exportwurzel für den angegebenen View-Key.
    /// </summary>
    /// <exception cref="ArgumentException">Unbekannter <paramref name="viewKey"/>.</exception>
    public static string GetViewFolderNameForViewKey(string viewKey)
    {
        if (viewKey.Equals("complete", StringComparison.Ordinal))
            return CompleteFolderName;
        if (viewKey.Equals("signatures-only", StringComparison.Ordinal))
            return SignaturesOnlyFolderName;
        if (viewKey.Equals("public-only", StringComparison.Ordinal))
            return PublicOnlyFolderName;
        if (viewKey.Equals("dto-only", StringComparison.Ordinal))
            return DtoOnlyFolderName;

        throw new ArgumentException($"Unbekannter View-Key: „{viewKey}“.", nameof(viewKey));
    }

    /// <summary>
    /// Bereinigt ein Namenssegment für die Verwendung in Dateinamen (Windows/Linux).
    /// Ungültige Zeichen werden durch <c>_</c> ersetzt; führende/nachfolgende Leerzeichen und
    /// nachfolgende Punkte/Leerzeichen (Windows) werden entfernt.
    /// </summary>
    public static string SanitizeFileNameSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment))
            return "unnamed";

        var invalid = Path.GetInvalidFileNameChars();
        Span<char> buffer = stackalloc char[segment.Length];
        var w = 0;
        foreach (var ch in segment)
        {
            buffer[w++] = Array.IndexOf(invalid, ch) >= 0 ? '_' : ch;
        }

        var s = new string(buffer[..w]).Trim();
        s = s.TrimEnd('.', ' ', '\t');

        if (string.IsNullOrEmpty(s))
            return "unnamed";

        return s;
    }

    /// <summary>
    /// Erzeugt den Dateinamen-Stamm (ohne <c>.md</c>) aus Solution- und Projekt-Anzeigenamen
    /// im Format <c>Solution.Project</c> — jeweils sanitisiert, ohne Kollisionsauflösung.
    /// </summary>
    public static string BuildSanitizedExportFileStem(string solutionDisplayName, string projectDisplayName)
    {
        var sol = SanitizeFileNameSegment(solutionDisplayName);
        var proj = SanitizeFileNameSegment(projectDisplayName);
        var stem = $"{sol}.{proj}";
        stem = EnsureNotReservedWindowsStem(stem);
        return stem;
    }

    /// <summary>
    /// Vergibt einen in <paramref name="usedFileStemsInView"/> noch nicht belegten Stamm
    /// (ohne <c>.md</c>): bei Kollision Suffixe <c>_2</c>, <c>_3</c>, …
    /// </summary>
    public static string AllocateUniqueFileStem(string sanitizedBaseStem, HashSet<string> usedFileStemsInView)
    {
        ArgumentNullException.ThrowIfNull(usedFileStemsInView);

        var stem = sanitizedBaseStem;
        if (usedFileStemsInView.Add(stem))
            return stem;

        for (var n = 2; ; n++)
        {
            stem = $"{sanitizedBaseStem}_{n}";
            if (usedFileStemsInView.Add(stem))
                return stem;
        }
    }

    /// <summary>
    /// Vollständiger Pfad zur Markdown-Zieldatei: <c>{outputRoot}/{viewFolderName}/{uniqueFileStemWithoutExtension}.md</c>.
    /// </summary>
    public static string GetViewOutputPath(
        string outputRoot,
        string viewFolderName,
        string uniqueFileStemWithoutExtension)
    {
        ArgumentException.ThrowIfNullOrEmpty(uniqueFileStemWithoutExtension);
        return Path.Combine(outputRoot, viewFolderName, uniqueFileStemWithoutExtension + ".md");
    }

    /// <summary>
    /// Wie <see cref="GetViewOutputPath(string, string, string)"/>, wobei der Stamm aus
    /// <see cref="BuildSanitizedExportFileStem"/> gebildet wird — ohne Kollisionsprüfung.
    /// </summary>
    public static string GetViewOutputPath(
        string outputRoot,
        string viewFolderName,
        string solutionDisplayName,
        string projectDisplayName) =>
        GetViewOutputPath(
            outputRoot,
            viewFolderName,
            BuildSanitizedExportFileStem(solutionDisplayName, projectDisplayName));

    private static string EnsureNotReservedWindowsStem(string stem) =>
        ReservedWindowsBaseNames.Contains(stem) ? stem + "_" : stem;
}
