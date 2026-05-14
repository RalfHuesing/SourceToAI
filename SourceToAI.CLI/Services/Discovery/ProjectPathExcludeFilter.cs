using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing;

namespace SourceToAI.CLI.Services.Discovery;

/// <summary>
/// Wertet <see cref="Configuration.AppSettings.ExcludedPathPatterns"/> relativ zum Projektstamm
/// mit <see cref="Matcher"/> aus und leitet Teilbaum-Pruning für <c>/**</c>-Muster ab.
/// </summary>
internal static class ProjectPathExcludeFilter
{
    /// <returns><see langword="null"/>, wenn keine Muster gesetzt sind.</returns>
    internal static ProjectPathExcludeSpec? TryCreate(string[]? patterns, string projectRoot)
    {
        if (patterns is null || patterns.Length == 0)
            return null;

        var rootFull = Path.GetFullPath(projectRoot);
        var normalized = new List<string>(patterns.Length);
        foreach (var raw in patterns)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            var n = NormalizePattern(raw);
            if (n.Length > 0)
                normalized.Add(n);
        }

        if (normalized.Count == 0)
            return null;

        var subtreePrefixes = new List<string>();
        foreach (var p in normalized)
        {
            if (TryGetSubtreePrefixFromDoubleStarPattern(p, out var prefix))
                subtreePrefixes.Add(prefix);
        }

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude("**/*");
        foreach (var p in normalized)
            matcher.AddExclude(p);

        return new ProjectPathExcludeSpec(
            rootFull,
            matcher,
            subtreePrefixes.Count > 0 ? subtreePrefixes : null);
    }

    private static bool TryGetSubtreePrefixFromDoubleStarPattern(string normalizedPattern, out string prefix)
    {
        const string doubleStar = "/**";
        if (!normalizedPattern.EndsWith(doubleStar, StringComparison.Ordinal))
        {
            prefix = string.Empty;
            return false;
        }

        prefix = normalizedPattern[..^doubleStar.Length].TrimEnd('/');
        return prefix.Length > 0;
    }

    /// <summary>Backslashes zu Schrägstrichen; keine führenden/abschließenden Schrägstriche.</summary>
    internal static string NormalizePattern(string pattern) =>
        pattern.Trim().Replace('\\', '/').Trim('/');

    internal static string RelativePathSlashes(string projectRoot, string absolutePath)
    {
        var rel = Path.GetRelativePath(projectRoot, absolutePath);
        return rel.Replace('\\', '/');
    }

    /// <summary>
    /// Prüft, ob <paramref name="absoluteDirectory"/> (als Verzeichnis) komplett unter einem
    /// <c>…/**</c>-Ausschluss liegt (Präfixgleichheit inkl. Pfadtrenner).
    /// </summary>
    internal static bool IsDirectorySubtreeExcluded(
        IReadOnlyList<string>? subtreePrefixes,
        string projectRoot,
        string absoluteDirectory)
    {
        if (subtreePrefixes is null || subtreePrefixes.Count == 0)
            return false;

        var root = Path.GetFullPath(projectRoot);
        var rel = RelativePathSlashes(root, Path.GetFullPath(absoluteDirectory));
        if (rel == "." || rel.Length == 0)
            return false;

        foreach (var prefix in subtreePrefixes)
        {
            if (rel.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
            if (rel.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    internal static bool IsFileExcludedByMatcher(Matcher matcher, string projectRoot, string absoluteFilePath)
    {
        var root = Path.GetFullPath(projectRoot);
        var fullFile = Path.GetFullPath(absoluteFilePath);
        var rel = RelativePathSlashes(root, fullFile);
        if (rel == "." || rel.Contains("..", StringComparison.Ordinal))
            return false;

        return !matcher.Match(root, rel).HasMatches;
    }
}

internal sealed class ProjectPathExcludeSpec(
    string projectRoot,
    Matcher matcher,
    IReadOnlyList<string>? subtreePrefixes)
{
    internal string ProjectRoot => projectRoot;
    internal Matcher Matcher => matcher;
    internal IReadOnlyList<string>? SubtreePrefixes => subtreePrefixes;
}
