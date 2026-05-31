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
    private const string CoreChildSegmentKey = "_Core";

    public IReadOnlyList<VirtualProjectPartition> PartitionProject(
        ProjectDefinition project,
        IReadOnlyList<string> absoluteFilePaths,
        int maxFileSizeKb,
        int maxFileCount,
        bool suppressCorePartition = true)
    {
        if (maxFileSizeKb <= 0 || maxFileCount <= 0 || absoluteFilePaths.Count == 0)
        {
            return [new VirtualProjectPartition(string.Empty, absoluteFilePaths)];
        }

        long maxSizeBytes = maxFileSizeKb * 1024L;

        var preliminaryCsPaths = absoluteFilePaths
            .Where(p => string.Equals(Path.GetExtension(p), ".cs", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFullPath)
            .ToList();

        if (preliminaryCsPaths.Count == 0)
        {
            return [new VirtualProjectPartition(string.Empty, absoluteFilePaths)];
        }

        var parseResult = csharpDocumentLoader.LoadParsedDocuments(project, preliminaryCsPaths);
        if (!parseResult.IsSuccess || parseResult.Value == null || parseResult.Value.Count == 0)
        {
            return [new VirtualProjectPartition(string.Empty, absoluteFilePaths)];
        }

        var parsedByPath = parseResult.Value.ToDictionary(
            d => Path.GetFullPath(d.AbsolutePath),
            d => d,
            StringComparer.OrdinalIgnoreCase);

        var dirNamespaceMap = ProjectSplittingFileClassifier.BuildDirectoryNamespaceMap(parseResult.Value);
        var classification = ProjectSplittingFileClassifier.Classify(
            absoluteFilePaths,
            parsedByPath,
            dirNamespaceMap);

        var assetPaths = classification.AssetPaths;
        var pathToSize = classification.EligibleNamespaceFiles
            .ToDictionary(f => Path.GetFullPath(f.Path), f => f.Size, StringComparer.OrdinalIgnoreCase);

        // 1. Namespace-Baum aufbauen
        var rootNode = new ProjectSplittingNamespaceNode(string.Empty, string.Empty);
        var allNodes = new List<ProjectSplittingNamespaceNode>();
        var coreNode = new ProjectSplittingNamespaceNode("Core", "Core");

        foreach (var file in classification.EligibleNamespaceFiles)
        {
            if (string.IsNullOrEmpty(file.Namespace))
            {
                coreNode.DirectFiles.Add(file.Path);
                coreNode.DirectSize += file.Size;
                continue;
            }

            AddFileToNamespaceTree(rootNode, allNodes, file.Path, file.Namespace, file.Size);
        }

        // 2. Buckets initialisieren
        var activeBuckets = new Dictionary<ProjectSplittingNamespaceNode, ProjectSplittingBucket>();
        foreach (var node in allNodes)
        {
            if (node.DirectFiles.Count > 0)
            {
                activeBuckets[node] = new ProjectSplittingBucket(node, node.DirectFiles, node.DirectSize);
            }
        }

        if (coreNode.DirectFiles.Count > 0)
        {
            activeBuckets[coreNode] = new ProjectSplittingBucket(
                coreNode,
                coreNode.DirectFiles,
                coreNode.DirectSize);
            AttachCoreNodeToTree(rootNode, allNodes, coreNode, activeBuckets);
        }

        bool IsCoreBucket(ProjectSplittingNamespaceNode node) =>
            string.Equals(node.FullNamespace, "Core", StringComparison.Ordinal);

        int CountBucketsTowardExportLimit() =>
            activeBuckets.Count(kv => !suppressCorePartition || !IsCoreBucket(kv.Key))
            + (assetPaths.Count > 0 ? 1 : 0);

        int CountNonCoreNamespaceBuckets() =>
            activeBuckets.Count(kv => !IsCoreBucket(kv.Key));

        // 3. Kollaps-Schleife (Harte Grenze erzwingen)
        while (CountBucketsTowardExportLimit() > maxFileCount && CountNonCoreNamespaceBuckets() > 1)
        {
            var candidateParents = allNodes
                .Where(n => ProjectSplittingCollapse.GetActiveBucketCountInSubtree(n, activeBuckets) >= 2)
                .ToList();

            if (candidateParents.Count == 0)
            {
                candidateParents = allNodes
                    .Where(n => ProjectSplittingCollapse.GetActiveBucketCountInSubtree(n, activeBuckets) >= 1
                        && activeBuckets.ContainsKey(n))
                    .ToList();
            }

            if (candidateParents.Count == 0)
                break;

            ProjectSplittingNamespaceNode? bestParent = null;
            int maxDepth = -1;
            long minSubtreeSize = long.MaxValue;

            foreach (var parent in candidateParents)
            {
                int depth = parent.FullNamespace.Split('.').Length;
                long subtreeSize = ProjectSplittingCollapse.GetSubtreeBucketSize(parent, activeBuckets);

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

            ProjectSplittingCollapse.CollapseTwoSmallestChildren(bestParent, activeBuckets);
        }

        // 4. Geschwister-Optimierung (Zusammenfassung kleiner Buckets unter maxFileSize)
        bool optimized = true;
        while (optimized)
        {
            optimized = false;
            foreach (var parent in allNodes)
            {
                var childrenBuckets = parent.Children
                    .Where(kv => !string.Equals(kv.Key, CoreChildSegmentKey, StringComparison.Ordinal))
                    .Select(kv => ProjectSplittingCollapse.GetActiveBucketInSubtreeOrSelf(kv.Value, activeBuckets))
                    .Where(b => b != null)
                    .Cast<ProjectSplittingBucket>()
                    .Distinct()
                    .ToList();

                if (childrenBuckets.Count >= 2)
                {
                    long combinedSize = childrenBuckets.Sum(b => b.Size);
                    long tinyThresholdBytes = Math.Min(maxSizeBytes / 5, 50 * 1024L);
                    if (combinedSize <= tinyThresholdBytes)
                    {
                        ProjectSplittingCollapse.CollapseNode(parent, activeBuckets);
                        optimized = true;
                        break;
                    }
                }
            }
        }

        if (suppressCorePartition)
        {
            AbsorbCorePartition(activeBuckets, pathToSize, maxSizeBytes, rootNode);
        }

        // 5. Partitionen erzeugen
        var partitions = new List<VirtualProjectPartition>();
        foreach (var bucket in activeBuckets.Values)
        {
            partitions.Add(new VirtualProjectPartition(bucket.Node.FullNamespace, bucket.FilePaths));
        }

        if (assetPaths.Count > 0)
        {
            partitions.Add(new VirtualProjectPartition("_Assets", assetPaths));
        }

        return partitions;
    }

    private static void AddFileToNamespaceTree(
        ProjectSplittingNamespaceNode rootNode,
        List<ProjectSplittingNamespaceNode> allNodes,
        string path,
        string ns,
        long size)
    {
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
                child = new ProjectSplittingNamespaceNode(segment, fullNs) { Parent = current };
                current.Children[segment] = child;
                allNodes.Add(child);
            }

            current = child;
        }

        current.DirectFiles.Add(path);
        current.DirectSize += size;
    }

    private static void AttachCoreNodeToTree(
        ProjectSplittingNamespaceNode rootNode,
        List<ProjectSplittingNamespaceNode> allNodes,
        ProjectSplittingNamespaceNode coreNode,
        Dictionary<ProjectSplittingNamespaceNode, ProjectSplittingBucket> activeBuckets)
    {
        ProjectSplittingNamespaceNode attachParent = rootNode;

        if (allNodes.Count > 0)
        {
            attachParent = allNodes
                .OrderBy(n => n.FullNamespace.Split('.').Length)
                .ThenBy(n => ProjectSplittingCollapse.GetSubtreeBucketSize(n, activeBuckets))
                .First();
        }

        attachParent.Children[CoreChildSegmentKey] = coreNode;
        coreNode.Parent = attachParent;
        allNodes.Add(coreNode);
    }

    private static void AbsorbCorePartition(
        Dictionary<ProjectSplittingNamespaceNode, ProjectSplittingBucket> activeBuckets,
        IReadOnlyDictionary<string, long> pathToSize,
        long maxSizeBytes,
        ProjectSplittingNamespaceNode rootNode)
    {
        ProjectSplittingNamespaceNode? coreNode = null;
        ProjectSplittingBucket? coreBucket = null;
        foreach (var (node, bucket) in activeBuckets)
        {
            if (string.Equals(node.FullNamespace, "Core", StringComparison.Ordinal))
            {
                coreNode = node;
                coreBucket = bucket;
                break;
            }
        }

        if (coreNode == null || coreBucket == null)
            return;

        var coreFiles = coreBucket.FilePaths.ToList();
        activeBuckets.Remove(coreNode);

        var otherNodes = activeBuckets.Keys.ToList();
        if (otherNodes.Count == 0)
        {
            var mergedSize = SumFileSizes(coreFiles, pathToSize);
            activeBuckets[rootNode] = new ProjectSplittingBucket(rootNode, coreFiles, mergedSize);
            return;
        }

        foreach (var filePath in coreFiles)
        {
            var fileSize = pathToSize.TryGetValue(filePath, out var size) ? size : 0L;
            var targetNode = SelectSmallestFittingBucketNode(activeBuckets, fileSize, maxSizeBytes);
            var current = activeBuckets[targetNode];
            var mergedPaths = current.FilePaths.ToList();
            mergedPaths.Add(filePath);
            activeBuckets[targetNode] = new ProjectSplittingBucket(
                targetNode,
                mergedPaths,
                current.Size + fileSize);
        }
    }

    private static ProjectSplittingNamespaceNode SelectSmallestFittingBucketNode(
        Dictionary<ProjectSplittingNamespaceNode, ProjectSplittingBucket> activeBuckets,
        long fileSize,
        long maxSizeBytes)
    {
        var ordered = activeBuckets.Values
            .OrderBy(b => b.Size)
            .ToList();

        var fitting = ordered.FirstOrDefault(b => b.Size + fileSize <= maxSizeBytes);
        return (fitting ?? ordered[0]).Node;
    }

    private static long SumFileSizes(IReadOnlyList<string> paths, IReadOnlyDictionary<string, long> pathToSize)
    {
        long total = 0;
        foreach (var path in paths)
        {
            if (pathToSize.TryGetValue(path, out var size))
                total += size;
        }

        return total;
    }
}
