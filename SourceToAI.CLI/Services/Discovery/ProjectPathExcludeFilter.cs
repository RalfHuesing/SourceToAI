using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing;

namespace SourceToAI.CLI.Services.Discovery;

/// <summary>
/// Wertet <see cref="Configuration.AppSettings.ExcludedPathPatterns"/> relativ zu einem Scan-Stamm
/// (Projektordner oder Solution-Wurzel) mit <see cref="Matcher"/> aus und leitet Teilbaum-Pruning ab.
/// </summary>
internal static class ProjectPathExcludeFilter
{
    /// <returns><see langword="null"/>, wenn keine Muster gesetzt sind.</returns>
    internal static ProjectPathExcludeSpec? TryCreate(string[]? patterns, string scanRoot)
    {
        if (patterns is null || patterns.Length == 0)
            return null;

        var rootFull = Path.GetFullPath(scanRoot);
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
        var matcherExcludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in normalized)
        {
            if (TryGetSubtreePrefix(p, out var prefix))
                subtreePrefixes.Add(prefix);

            matcherExcludes.Add(p);
            if (IsLiteralPathPattern(p))
                matcherExcludes.Add(p + "/**");
        }

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude("**/*");
        foreach (var p in matcherExcludes)
            matcher.AddExclude(p);

        return new ProjectPathExcludeSpec(
            rootFull,
            matcher,
            subtreePrefixes.Count > 0 ? subtreePrefixes : null);
    }

    /// <summary>Literaler Pfad ohne Glob-Zeichen — schließt den gesamten Unterbaum ein.</summary>
    private static bool IsLiteralPathPattern(string normalizedPattern) =>
        normalizedPattern.Length > 0
        && !normalizedPattern.Contains('*', StringComparison.Ordinal)
        && !normalizedPattern.Contains('?', StringComparison.Ordinal);

    private static bool TryGetSubtreePrefix(string normalizedPattern, out string prefix)
    {
        const string doubleStar = "/**";
        if (normalizedPattern.EndsWith(doubleStar, StringComparison.Ordinal))
        {
            prefix = normalizedPattern[..^doubleStar.Length].TrimEnd('/');
            return prefix.Length > 0;
        }

        if (IsLiteralPathPattern(normalizedPattern))
        {
            prefix = normalizedPattern;
            return true;
        }

        prefix = string.Empty;
        return false;
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

    internal static bool IsFileExcludedByMatcher(Matcher matcher, string scanRoot, string absoluteFilePath)
    {
        var root = Path.GetFullPath(scanRoot);
        var fullFile = Path.GetFullPath(absoluteFilePath);
        var rel = RelativePathSlashes(root, fullFile);
        if (rel == "." || rel.Contains("..", StringComparison.Ordinal))
            return false;

        return !matcher.Match(root, rel).HasMatches;
    }

    internal static bool IsDirectoryExcluded(
        ProjectPathExcludeSpec? spec,
        string absoluteDirectory)
    {
        if (spec is null)
            return false;

        return IsDirectorySubtreeExcluded(spec.SubtreePrefixes, spec.ProjectRoot, absoluteDirectory);
    }

    internal static bool IsFileExcluded(ProjectPathExcludeSpec? spec, string absoluteFilePath)
    {
        if (spec is null)
            return false;

        return IsFileExcludedByMatcher(spec.Matcher, spec.ProjectRoot, absoluteFilePath);
    }

    internal static bool IsPathExcludedByAny(
        ProjectPathExcludeSpec? primary,
        ProjectPathExcludeSpec? secondary,
        string absolutePath,
        bool isDirectory)
    {
        if (isDirectory)
        {
            return IsDirectoryExcluded(primary, absolutePath)
                || IsDirectoryExcluded(secondary, absolutePath);
        }

        return IsFileExcluded(primary, absolutePath)
            || IsFileExcluded(secondary, absolutePath);
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
