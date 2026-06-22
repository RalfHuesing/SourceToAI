using System.Text.RegularExpressions;
using SourceToAI.CLI.App.Exceptions;

namespace SourceToAI.CLI.App.Cli;

/// <summary>
/// Aufgelöste GAC-Assembly inkl. Metadaten für Logging.
/// </summary>
internal readonly record struct GacResolvedAssembly(
    string FullPath,
    string SimpleName,
    Version Version,
    string FlavorLabel);

internal readonly record struct GacFlavor(string Root, string Label, int Rank);

internal readonly struct GacScanState(
    IReadOnlyList<string> dllFilePatterns,
    bool[] patternHadHit,
    List<GacCandidate> candidates)
{
    public IReadOnlyList<string> DllFilePatterns { get; } = dllFilePatterns;
    public bool[] PatternHadHit { get; } = patternHadHit;
    public List<GacCandidate> Candidates { get; } = candidates;
}

internal readonly record struct GacCandidate(
    string FullPath,
    string SimpleName,
    Version Version,
    string TokenKey,
    string FlavorLabel,
    int FlavorRank);

/// <summary>
/// Findet Assembly-DLLs im .NET-4+-GAC anhand von Dateinamen-Mustern; pro Assembly die höchste Version, MSIL vor 32 vor 64.
/// </summary>
internal static class GacAssemblyDiscovery
{
    private static readonly Regex VersionFolderPattern = new(
        @"^v\d+\.\d+_(?<version>[\d.]+)__(?<token>[0-9a-fA-F]{16})$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));

    /// <summary>
    /// Löst <paramref name="dllFilePatterns"/> im GAC auf (z. B. <c>Contoso.*.dll</c>).
    /// </summary>
    /// <param name="dllFilePatterns">Dateinamen-Muster für <see cref="Directory.GetFiles(string, string)"/>.</param>
    /// <param name="assemblyRoot">Vollständiger Pfad zum <c>assembly</c>-Ordner; für Tests überschreibbar.</param>
    internal static IReadOnlyList<GacResolvedAssembly> Resolve(
        IReadOnlyList<string> dllFilePatterns,
        string assemblyRoot)
    {
        if (dllFilePatterns.Count == 0)
            return Array.Empty<GacResolvedAssembly>();

        var flavorRoots = GacPathResolver.ResolveFlavorRoots(assemblyRoot);
        if (flavorRoots.Count == 0)
        {
            throw new SourceToAiValidationException(
                $"Unter \"{assemblyRoot}\" wurden keine GAC-Flavor-Ordner (GAC_MSIL, GAC_32, GAC_64) gefunden.");
        }

        var patternHadHit = new bool[dllFilePatterns.Count];
        var candidates = new List<GacCandidate>();
        var state = new GacScanState(dllFilePatterns, patternHadHit, candidates);

        foreach (var (flavorLabel, flavorRoot, rank) in flavorRoots)
        {
            CollectFromFlavor(new GacFlavor(flavorRoot, flavorLabel, rank), state);
        }

        for (var i = 0; i < dllFilePatterns.Count; i++)
        {
            if (!patternHadHit[i])
            {
                throw new SourceToAiValidationException(
                    $"Keine GAC-Treffer für Muster: \"{dllFilePatterns[i]}\".");
            }
        }

        if (candidates.Count == 0)
        {
            var patternList = string.Join(", ", dllFilePatterns.Select(static p => $"\"{p}\""));
            throw new SourceToAiValidationException(
                $"Keine GAC-Treffer für Muster: {patternList}.");
        }

        return candidates
            .GroupBy(static c => (c.SimpleName, c.Version, c.TokenKey))
            .Select(g => g.OrderBy(static c => c.FlavorRank).First())
            .OrderBy(static c => c.SimpleName, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(static c => c.Version)
            .ThenBy(static c => c.FullPath, StringComparer.OrdinalIgnoreCase)
            .Select(static c => new GacResolvedAssembly(c.FullPath, c.SimpleName, c.Version, c.FlavorLabel))
            .ToList();
    }

    private static void CollectFromFlavor(GacFlavor flavor, GacScanState state)
    {
        foreach (var assemblyDir in Directory.EnumerateDirectories(flavor.Root))
        {
            var simpleName = Path.GetFileName(assemblyDir);
            if (string.IsNullOrEmpty(simpleName))
                continue;

            ScanAssemblyDir(assemblyDir, simpleName, flavor, state);
        }
    }

    private static void ScanAssemblyDir(
        string assemblyDir,
        string simpleName,
        GacFlavor flavor,
        GacScanState state)
    {
        var versionDirs = new List<(Version Version, string Token, string FullPath)>();
        foreach (var versionDir in Directory.EnumerateDirectories(assemblyDir))
        {
            var folderName = Path.GetFileName(versionDir);
            if (string.IsNullOrEmpty(folderName))
                continue;

            var match = VersionFolderPattern.Match(folderName);
            if (!match.Success)
                continue;

            if (!Version.TryParse(match.Groups["version"].Value, out var version))
                continue;

            var token = match.Groups["token"].Value;
            versionDirs.Add((version, token, versionDir));
        }

        if (versionDirs.Count == 0)
            return;

        ScanVersionDirectories(versionDirs, simpleName, flavor, state);
    }

    private static void ScanVersionDirectories(
        List<(Version Version, string Token, string FullPath)> versionDirs,
        string simpleName,
        GacFlavor flavor,
        GacScanState state)
    {
        foreach (var tokenGroup in versionDirs.GroupBy(static v => v.Token, StringComparer.OrdinalIgnoreCase))
        {
            var (_, token, versionDirPath) = tokenGroup.OrderByDescending(static x => x.Version).First();
            var pathsForVersion = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var patternIndex = 0; patternIndex < state.DllFilePatterns.Count; patternIndex++)
            {
                var pattern = state.DllFilePatterns[patternIndex];
                foreach (var file in Directory.GetFiles(versionDirPath, pattern))
                {
                    pathsForVersion.Add(Path.GetFullPath(file));
                    state.PatternHadHit[patternIndex] = true;
                }
            }

            var tokenKey = token.ToUpperInvariant();
            foreach (var fullPath in pathsForVersion)
            {
                var version = tokenGroup.OrderByDescending(static x => x.Version).First().Version;
                state.Candidates.Add(new GacCandidate(
                    fullPath,
                    simpleName,
                    version,
                    tokenKey,
                    flavor.Label,
                    flavor.Rank));
            }
        }
    }
}
