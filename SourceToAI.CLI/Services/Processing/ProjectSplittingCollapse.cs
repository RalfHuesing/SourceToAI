namespace SourceToAI.CLI.Services.Processing;

internal static class ProjectSplittingCollapse
{
    internal static int GetActiveBucketCountInSubtree(
        ProjectSplittingNamespaceNode node,
        Dictionary<ProjectSplittingNamespaceNode, ProjectSplittingBucket> activeBuckets)
    {
        int count = activeBuckets.ContainsKey(node) ? 1 : 0;
        foreach (var child in node.Children.Values)
            count += GetActiveBucketCountInSubtree(child, activeBuckets);
        return count;
    }

    internal static long GetSubtreeBucketSize(
        ProjectSplittingNamespaceNode node,
        Dictionary<ProjectSplittingNamespaceNode, ProjectSplittingBucket> activeBuckets)
    {
        long size = activeBuckets.TryGetValue(node, out var b) ? b.Size : 0;
        foreach (var child in node.Children.Values)
            size += GetSubtreeBucketSize(child, activeBuckets);
        return size;
    }

    internal static ProjectSplittingBucket? GetActiveBucketInSubtreeOrSelf(
        ProjectSplittingNamespaceNode node,
        Dictionary<ProjectSplittingNamespaceNode, ProjectSplittingBucket> activeBuckets)
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

    internal static void CollapseNode(
        ProjectSplittingNamespaceNode parent,
        Dictionary<ProjectSplittingNamespaceNode, ProjectSplittingBucket> activeBuckets)
    {
        var subtreeBuckets = new List<ProjectSplittingBucket>();
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

        activeBuckets[parent] = new ProjectSplittingBucket(parent, mergedPaths, mergedSize);
    }

    internal static void CollapseTwoSmallestChildren(
        ProjectSplittingNamespaceNode parent,
        Dictionary<ProjectSplittingNamespaceNode, ProjectSplittingBucket> activeBuckets)
    {
        var subtreeBuckets = new List<ProjectSplittingBucket>();
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
            activeBuckets[parent] = new ProjectSplittingBucket(
                parent,
                existingParentBucket.FilePaths,
                existingParentBucket.Size + mergedSize);
        }
        else
        {
            activeBuckets[parent] = new ProjectSplittingBucket(parent, mergedPaths, mergedSize);
        }
    }

    private static void CollectActiveBucketsInSubtree(
        ProjectSplittingNamespaceNode node,
        Dictionary<ProjectSplittingNamespaceNode, ProjectSplittingBucket> activeBuckets,
        List<ProjectSplittingBucket> results)
    {
        if (activeBuckets.TryGetValue(node, out var b))
            results.Add(b);

        foreach (var child in node.Children.Values)
            CollectActiveBucketsInSubtree(child, activeBuckets, results);
    }
}
