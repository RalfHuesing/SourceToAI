using SourceToAI.CLI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SourceToAI.CLI.Services.Processing;

internal sealed record NamespaceEligibleFile(string Path, string Namespace, long Size);

internal sealed record ClassificationResult(
    IReadOnlyList<string> CsPaths,
    List<NamespaceEligibleFile> EligibleNamespaceFiles,
    List<string> AssetPaths);

/// <summary>
/// Ordnet Projektdateien Namespace-Partitionen oder dem Asset-Bucket zu (UI, Stamm-Begleiter, Rest).
/// </summary>
internal static class ProjectSplittingFileClassifier
{
    private static readonly HashSet<string> UiExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".razor",
        ".xaml",
        ".cshtml"
    };

    private readonly struct ClassificationState(
        IReadOnlyDictionary<string, ParsedCSharpDocument> parsedByPath,
        IReadOnlyDictionary<string, string> dirNamespaceMap,
        Dictionary<string, HashSet<string>> anchorsByDir)
    {
        public IReadOnlyDictionary<string, ParsedCSharpDocument> ParsedByPath { get; } = parsedByPath;
        public IReadOnlyDictionary<string, string> DirNamespaceMap { get; } = dirNamespaceMap;
        public Dictionary<string, HashSet<string>> AnchorsByDir { get; } = anchorsByDir;
    }

    internal static Dictionary<string, string> BuildDirectoryNamespaceMap(
        IReadOnlyList<ParsedCSharpDocument> parsedDocuments)
    {
        return parsedDocuments
            .Select(doc => (Dir: NormalizeDirectory(Path.GetFullPath(doc.AbsolutePath)), Ns: NamespaceExtractor.GetNamespace(doc.Root)))
            .Where(x => !string.IsNullOrEmpty(x.Dir) && !string.IsNullOrEmpty(x.Ns))
            .GroupBy(x => x.Dir!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Ns, StringComparer.OrdinalIgnoreCase);
    }

    internal static ClassificationResult Classify(
        IReadOnlyList<string> absoluteFilePaths,
        IReadOnlyDictionary<string, ParsedCSharpDocument> parsedByPath,
        IReadOnlyDictionary<string, string> dirNamespaceMap)
    {
        var csPaths = absoluteFilePaths
            .Where(p => string.Equals(Path.GetExtension(p), ".cs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var anchorsByDir = BuildAnchorsByDir(absoluteFilePaths, dirNamespaceMap);
        var eligible = new List<NamespaceEligibleFile>();
        var assetPaths = new List<string>();

        var state = new ClassificationState(parsedByPath, dirNamespaceMap, anchorsByDir);
        CategorizePaths(absoluteFilePaths, state, eligible, assetPaths);

        return new ClassificationResult(csPaths, eligible, assetPaths);
    }

    private static void AddAnchorIfEligible(
        string path,
        IReadOnlyDictionary<string, string> dirNamespaceMap,
        Dictionary<string, HashSet<string>> anchorsByDir)
    {
        var fullPath = Path.GetFullPath(path);
        var dir = NormalizeDirectory(fullPath);
        if (dir == null || !dirNamespaceMap.ContainsKey(dir))
            return;

        var ext = Path.GetExtension(fullPath);
        if (string.Equals(ext, ".cs", StringComparison.OrdinalIgnoreCase) || UiExtensions.Contains(ext))
        {
            if (!anchorsByDir.TryGetValue(dir, out var anchors))
            {
                anchors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                anchorsByDir[dir] = anchors;
            }

            var anchor = string.Equals(ext, ".cs", StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileNameWithoutExtension(fullPath)
                : Path.GetFileName(fullPath);
            if (!string.IsNullOrEmpty(anchor))
                anchors.Add(anchor);
        }
    }

    private static Dictionary<string, HashSet<string>> BuildAnchorsByDir(
        IReadOnlyList<string> absoluteFilePaths,
        IReadOnlyDictionary<string, string> dirNamespaceMap)
    {
        var anchorsByDir = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in absoluteFilePaths)
        {
            AddAnchorIfEligible(path, dirNamespaceMap, anchorsByDir);
        }
        return anchorsByDir;
    }

    private static void CategorizePath(
        string path,
        ClassificationState state,
        List<NamespaceEligibleFile> eligible,
        List<string> assetPaths)
    {
        var fullPath = Path.GetFullPath(path);
        var ext = Path.GetExtension(fullPath);

        if (string.Equals(ext, ".cs", StringComparison.OrdinalIgnoreCase))
        {
            if (state.ParsedByPath.TryGetValue(fullPath, out var parsedDoc))
            {
                var ns = NamespaceExtractor.GetNamespace(parsedDoc.Root);
                eligible.Add(new NamespaceEligibleFile(fullPath, ns, parsedDoc.SizeBytes));
            }
            return;
        }

        if (UiExtensions.Contains(ext))
        {
            var dir = NormalizeDirectory(fullPath);
            var ns = dir != null && state.DirNamespaceMap.TryGetValue(dir, out var mappedNs) ? mappedNs : string.Empty;
            eligible.Add(new NamespaceEligibleFile(fullPath, ns, GetFileSizeBytes(fullPath)));
            return;
        }

        CategorizeOtherPath(fullPath, ext, state, eligible, assetPaths);
    }

    private static void CategorizeOtherPath(
        string fullPath,
        string ext,
        ClassificationState state,
        List<NamespaceEligibleFile> eligible,
        List<string> assetPaths)
    {
        var dir = NormalizeDirectory(fullPath);
        if (dir != null && state.DirNamespaceMap.TryGetValue(dir, out var mappedNs))
        {
            bool isMarkdownDoc = string.Equals(ext, ".md", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(ext, ".mdc", StringComparison.OrdinalIgnoreCase);

            if (isMarkdownDoc || (state.AnchorsByDir.TryGetValue(dir, out var anchors) && IsStemCompanion(Path.GetFileName(fullPath), anchors)))
            {
                eligible.Add(new NamespaceEligibleFile(fullPath, mappedNs, GetFileSizeBytes(fullPath)));
                return;
            }
        }

        assetPaths.Add(fullPath);
    }

    private static void CategorizePaths(
        IReadOnlyList<string> absoluteFilePaths,
        ClassificationState state,
        List<NamespaceEligibleFile> eligible,
        List<string> assetPaths)
    {
        foreach (var path in absoluteFilePaths)
        {
            CategorizePath(path, state, eligible, assetPaths);
        }
    }

    private static bool IsStemCompanion(string fileName, HashSet<string> anchors)
    {
        foreach (var anchor in anchors)
        {
            if (fileName.StartsWith(anchor + ".", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string? NormalizeDirectory(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        return string.IsNullOrEmpty(dir) ? null : Path.GetFullPath(dir);
    }

    private static long GetFileSizeBytes(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (IOException)
        {
            return 0L;
        }
        catch (UnauthorizedAccessException)
        {
            return 0L;
        }
    }
}
