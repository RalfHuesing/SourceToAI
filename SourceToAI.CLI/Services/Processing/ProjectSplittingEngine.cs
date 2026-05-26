using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SourceToAI.CLI.Services.Processing;

/// <summary>
/// Modell für eine Projektpartition.
/// </summary>
public sealed class VirtualProjectPartition(string subNamespaceName, IReadOnlyList<string> paths)
{
    public string SubNamespaceName { get; } = subNamespaceName;
    public IReadOnlyList<string> Paths { get; } = paths;
}

/// <summary>
/// Engine zum adaptiven Splitting von C#-Projekten basierend auf Namespaces und Dateigrößen-Limits.
/// </summary>
public sealed class ProjectSplittingEngine(ICSharpDocumentLoader csharpDocumentLoader)
{
    public IReadOnlyList<VirtualProjectPartition> PartitionProject(
        ProjectDefinition project,
        IReadOnlyList<string> absoluteFilePaths,
        int maxFileSizeKb,
        int maxFileCount)
    {
        if (maxFileSizeKb <= 0 || maxFileCount <= 0 || absoluteFilePaths.Count == 0)
        {
            return [new VirtualProjectPartition(string.Empty, absoluteFilePaths)];
        }

        long maxSizeBytes = maxFileSizeKb * 1024L;

        // C#- und Asset-Dateien trennen (Option A für Frage 1)
        var csPaths = new List<string>();
        var assetPaths = new List<string>();

        foreach (var path in absoluteFilePaths)
        {
            if (string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
                csPaths.Add(path);
            else
                assetPaths.Add(path);
        }

        if (csPaths.Count == 0)
        {
            return [new VirtualProjectPartition(string.Empty, absoluteFilePaths)];
        }

        var parseResult = csharpDocumentLoader.LoadParsedDocuments(project, csPaths);
        if (!parseResult.IsSuccess || parseResult.Value == null || parseResult.Value.Count == 0)
        {
            return [new VirtualProjectPartition(string.Empty, absoluteFilePaths)];
        }

        // 1. Namespace-Baum aufbauen
        var rootNode = new NamespaceNode(string.Empty, string.Empty);
        var allNodes = new List<NamespaceNode>();
        var coreFiles = new List<(string Path, long Size)>(); // Option A für Frage 2 (Global/Root)

        foreach (var parsedDoc in parseResult.Value)
        {
            var ns = NamespaceExtractor.GetNamespace(parsedDoc.Root);
            if (string.IsNullOrEmpty(ns))
            {
                coreFiles.Add((parsedDoc.AbsolutePath, parsedDoc.SizeBytes));
                continue;
            }

            var segments = ns.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var current = rootNode;
            var currentNs = new StringBuilder();

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (currentNs.Length > 0)
                    currentNs.Append('.');
                currentNs.Append(segment);

                var fullNs = currentNs.ToString();

                if (!current.Children.TryGetValue(segment, out var child))
                {
                    child = new NamespaceNode(segment, fullNs) { Parent = current };
                    current.Children[segment] = child;
                    allNodes.Add(child);
                }
                current = child;
            }

            current.DirectFiles.Add(parsedDoc.AbsolutePath);
            current.DirectSize += parsedDoc.SizeBytes;
        }

        // 2. Buckets initialisieren
        var activeBuckets = new Dictionary<NamespaceNode, Bucket>();
        foreach (var node in allNodes)
        {
            if (node.DirectFiles.Count > 0)
            {
                activeBuckets[node] = new Bucket(node, node.DirectFiles, node.DirectSize);
            }
        }

        Bucket? coreBucket = null;
        if (coreFiles.Count > 0)
        {
            var coreNode = new NamespaceNode("Core", "Core");
            coreBucket = new Bucket(coreNode, coreFiles.Select(f => f.Path).ToList(), coreFiles.Sum(f => f.Size));
        }

        int GetTotalBucketCount() => activeBuckets.Count + (coreBucket != null ? 1 : 0) + (assetPaths.Count > 0 ? 1 : 0);

        // 3. Kollaps-Schleife (Harte Grenze erzwingen)
        while (GetTotalBucketCount() > maxFileCount && activeBuckets.Count > 1)
        {
            NamespaceNode? bestParent = null;
            int maxDepth = -1;
            long minSubtreeSize = long.MaxValue;

            var candidateParents = allNodes
                .Where(n => GetActiveBucketCountInSubtree(n, activeBuckets) >= 2)
                .ToList();

            if (candidateParents.Count == 0)
            {
                candidateParents = allNodes
                    .Where(n => GetActiveBucketCountInSubtree(n, activeBuckets) >= 1 && activeBuckets.ContainsKey(n))
                    .ToList();
            }

            if (candidateParents.Count == 0)
                break;

            foreach (var parent in candidateParents)
            {
                int depth = parent.FullNamespace.Split('.').Length;
                long subtreeSize = GetSubtreeBucketSize(parent, activeBuckets);

                if (depth > maxDepth)
                {
                    maxDepth = depth;
                    minSubtreeSize = subtreeSize;
                    bestParent = parent;
                }
                else if (depth == maxDepth && subtreeSize < minSubtreeSize)
                {
                    minSubtreeSize = subtreeSize;
                    bestParent = parent;
                }
            }

            if (bestParent == null)
                break;

            CollapseTwoSmallestChildren(bestParent, activeBuckets);
        }

        // 4. Geschwister-Optimierung (Zusammenfassung kleiner Buckets unter maxFileSize)
        bool optimized = true;
        while (optimized)
        {
            optimized = false;
            foreach (var parent in allNodes)
            {
                var childrenBuckets = parent.Children.Values
                    .Select(c => GetActiveBucketInSubtreeOrSelf(c, activeBuckets))
                    .Where(b => b != null)
                    .Cast<Bucket>()
                    .Distinct()
                    .ToList();

                if (childrenBuckets.Count >= 2)
                {
                    long combinedSize = childrenBuckets.Sum(b => b.Size);
                    // Sibling-Optimierung nur fuer sehr kleine Buckets (z. B. unter 20% des Limits oder max. 50 KB),
                    // damit fachliche Splits bei ausreichend grossen Namespaces erhalten bleiben.
                    long tinyThresholdBytes = Math.Min(maxSizeBytes / 5, 50 * 1024L);
                    if (combinedSize <= tinyThresholdBytes)
                    {
                        CollapseNode(parent, activeBuckets);
                        optimized = true;
                        break;
                    }
                }
            }
        }

        // 5. Partitionen erzeugen
        var partitions = new List<VirtualProjectPartition>();
        foreach (var bucket in activeBuckets.Values)
        {
            string partitionName = bucket.Node.FullNamespace;
            if (string.IsNullOrEmpty(partitionName))
                partitionName = "Core";

            partitions.Add(new VirtualProjectPartition(partitionName, bucket.FilePaths));
        }

        if (coreBucket != null)
        {
            partitions.Add(new VirtualProjectPartition("Core", coreBucket.FilePaths));
        }

        if (assetPaths.Count > 0)
        {
            partitions.Add(new VirtualProjectPartition("_Assets", assetPaths));
        }

        return partitions;
    }

    private static int GetActiveBucketCountInSubtree(NamespaceNode node, Dictionary<NamespaceNode, Bucket> activeBuckets)
    {
        int count = activeBuckets.ContainsKey(node) ? 1 : 0;
        foreach (var child in node.Children.Values)
        {
            count += GetActiveBucketCountInSubtree(child, activeBuckets);
        }
        return count;
    }

    private static long GetSubtreeBucketSize(NamespaceNode node, Dictionary<NamespaceNode, Bucket> activeBuckets)
    {
        long size = activeBuckets.TryGetValue(node, out var b) ? b.Size : 0;
        foreach (var child in node.Children.Values)
        {
            size += GetSubtreeBucketSize(child, activeBuckets);
        }
        return size;
    }

    private static Bucket? GetActiveBucketInSubtreeOrSelf(NamespaceNode node, Dictionary<NamespaceNode, Bucket> activeBuckets)
    {
        if (activeBuckets.TryGetValue(node, out var b))
            return b;

        foreach (var child in node.Children.Values)
        {
            var result = GetActiveBucketInSubtreeOrSelf(child, activeBuckets);
            if (result != null)
                return result;
        }
        return null;
    }

    private static void CollapseNode(NamespaceNode parent, Dictionary<NamespaceNode, Bucket> activeBuckets)
    {
        var subtreeBuckets = new List<Bucket>();
        CollectActiveBucketsInSubtree(parent, activeBuckets, subtreeBuckets);

        if (subtreeBuckets.Count == 0)
            return;

        var mergedPaths = new List<string>();
        long mergedSize = 0;

        foreach (var bucket in subtreeBuckets)
        {
            mergedPaths.AddRange(bucket.FilePaths);
            mergedSize += bucket.Size;
            activeBuckets.Remove(bucket.Node);
        }

        activeBuckets[parent] = new Bucket(parent, mergedPaths, mergedSize);
    }

    private static void CollapseTwoSmallestChildren(NamespaceNode parent, Dictionary<NamespaceNode, Bucket> activeBuckets)
    {
        var subtreeBuckets = new List<Bucket>();
        CollectActiveBucketsInSubtree(parent, activeBuckets, subtreeBuckets);

        if (subtreeBuckets.Count < 2)
            return;

        var sorted = subtreeBuckets.OrderBy(b => b.Size).ToList();
        var b1 = sorted[0];
        var b2 = sorted[1];

        var mergedPaths = b1.FilePaths.Concat(b2.FilePaths).ToList();
        long mergedSize = b1.Size + b2.Size;

        activeBuckets.Remove(b1.Node);
        activeBuckets.Remove(b2.Node);

        if (activeBuckets.TryGetValue(parent, out var existingParentBucket))
        {
            existingParentBucket.FilePaths.AddRange(mergedPaths);
            activeBuckets[parent] = new Bucket(parent, existingParentBucket.FilePaths, existingParentBucket.Size + mergedSize);
        }
        else
        {
            activeBuckets[parent] = new Bucket(parent, mergedPaths, mergedSize);
        }
    }

    private static void CollectActiveBucketsInSubtree(NamespaceNode node, Dictionary<NamespaceNode, Bucket> activeBuckets, List<Bucket> results)
    {
        if (activeBuckets.TryGetValue(node, out var b))
        {
            results.Add(b);
        }
        foreach (var child in node.Children.Values)
        {
            CollectActiveBucketsInSubtree(child, activeBuckets, results);
        }
    }

    private sealed class NamespaceNode(string segmentName, string fullNamespace)
    {
        public string SegmentName { get; } = segmentName;
        public string FullNamespace { get; } = fullNamespace;
        public List<string> DirectFiles { get; } = new();
        public long DirectSize { get; set; }
        public Dictionary<string, NamespaceNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
        public NamespaceNode? Parent { get; set; }
    }

    private sealed class Bucket(NamespaceNode node, List<string> filePaths, long size)
    {
        public NamespaceNode Node { get; } = node;
        public List<string> FilePaths { get; } = filePaths;
        public long Size { get; } = size;
    }
}
