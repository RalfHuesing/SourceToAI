using SourceToAI.CLI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SourceToAI.CLI.Services.Processing;

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

    internal sealed record NamespaceEligibleFile(string Path, string Namespace, long Size);

    internal sealed record ClassificationResult(
        IReadOnlyList<string> CsPaths,
        List<NamespaceEligibleFile> EligibleNamespaceFiles,
        List<string> AssetPaths);

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
        var csPaths = new List<string>();
        var eligible = new List<NamespaceEligibleFile>();
        var assetPaths = new List<string>();
        var anchorsByDir = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in absoluteFilePaths)
        {
            var ext = Path.GetExtension(path);
            if (string.Equals(ext, ".cs", StringComparison.OrdinalIgnoreCase))
                csPaths.Add(path);
        }

        foreach (var path in absoluteFilePaths)
        {
            var fullPath = Path.GetFullPath(path);
            var dir = NormalizeDirectory(fullPath);
            if (dir == null || !dirNamespaceMap.ContainsKey(dir))
                continue;

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

        foreach (var path in absoluteFilePaths)
        {
            var fullPath = Path.GetFullPath(path);
            var ext = Path.GetExtension(fullPath);
            var isCs = string.Equals(ext, ".cs", StringComparison.OrdinalIgnoreCase);

            if (isCs)
            {
                if (!parsedByPath.TryGetValue(fullPath, out var parsedDoc))
                    continue;

                var ns = NamespaceExtractor.GetNamespace(parsedDoc.Root);
                eligible.Add(new NamespaceEligibleFile(fullPath, ns, parsedDoc.SizeBytes));
            }
            else if (UiExtensions.Contains(ext))
            {
                var dir = NormalizeDirectory(fullPath);
                var ns = dir != null && dirNamespaceMap.TryGetValue(dir, out var mappedNs) ? mappedNs : string.Empty;
                eligible.Add(new NamespaceEligibleFile(fullPath, ns, GetFileSizeBytes(fullPath)));
            }
            else
            {
                var dir = NormalizeDirectory(fullPath);
                if (dir != null && dirNamespaceMap.TryGetValue(dir, out var mappedNs))
                {
                    bool isMarkdownDoc = string.Equals(ext, ".md", StringComparison.OrdinalIgnoreCase)
                                         || string.Equals(ext, ".mdc", StringComparison.OrdinalIgnoreCase);

                    if (isMarkdownDoc || (anchorsByDir.TryGetValue(dir, out var anchors) && IsStemCompanion(Path.GetFileName(fullPath), anchors)))
                    {
                        eligible.Add(new NamespaceEligibleFile(fullPath, mappedNs, GetFileSizeBytes(fullPath)));
                    }
                    else
                    {
                        assetPaths.Add(fullPath);
                    }
                }
                else
                {
                    assetPaths.Add(fullPath);
                }
            }
        }

        return new ClassificationResult(csPaths, eligible, assetPaths);
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
