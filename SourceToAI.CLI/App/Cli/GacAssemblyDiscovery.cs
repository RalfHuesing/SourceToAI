using System.Text.RegularExpressions;
using SourceToAI.CLI.App.Exceptions;

namespace SourceToAI.CLI.App.Cli;

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
    /// Aufgelöste GAC-Assembly inkl. Metadaten für Logging.
    /// </summary>
    internal readonly record struct GacResolvedAssembly(
        string FullPath,
        string SimpleName,
        Version Version,
        string FlavorLabel);

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

        foreach (var (flavorLabel, flavorRoot, rank) in flavorRoots)
        {
            CollectFromFlavor(
                flavorRoot,
                flavorLabel,
                rank,
                dllFilePatterns,
                patternHadHit,
                candidates);
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

    private static void CollectFromFlavor(
        string flavorRoot,
        string flavorLabel,
        int flavorRank,
        IReadOnlyList<string> dllFilePatterns,
        bool[] patternHadHit,
        List<GacCandidate> candidates)
    {
        foreach (var assemblyDir in Directory.EnumerateDirectories(flavorRoot))
        {
            var simpleName = Path.GetFileName(assemblyDir);
            if (string.IsNullOrEmpty(simpleName))
                continue;

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
                continue;

            foreach (var tokenGroup in versionDirs.GroupBy(static v => v.Token, StringComparer.OrdinalIgnoreCase))
            {
                var (_, token, versionDirPath) = tokenGroup.OrderByDescending(static x => x.Version).First();
                var pathsForVersion = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (var patternIndex = 0; patternIndex < dllFilePatterns.Count; patternIndex++)
                {
                    var pattern = dllFilePatterns[patternIndex];
                    foreach (var file in Directory.GetFiles(versionDirPath, pattern))
                    {
                        pathsForVersion.Add(Path.GetFullPath(file));
                        patternHadHit[patternIndex] = true;
                    }
                }

                var tokenKey = token.ToUpperInvariant();
                foreach (var fullPath in pathsForVersion)
                {
                    var version = tokenGroup.OrderByDescending(static x => x.Version).First().Version;
                    candidates.Add(new GacCandidate(
                        fullPath,
                        simpleName,
                        version,
                        tokenKey,
                        flavorLabel,
                        flavorRank));
                }
            }
        }
    }

    private readonly record struct GacCandidate(
        string FullPath,
        string SimpleName,
        Version Version,
        string TokenKey,
        string FlavorLabel,
        int FlavorRank);
}
