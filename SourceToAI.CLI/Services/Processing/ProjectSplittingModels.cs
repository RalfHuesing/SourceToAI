namespace SourceToAI.CLI.Services.Processing;

internal sealed class ProjectSplittingNamespaceNode(string segmentName, string fullNamespace)
{
    public string SegmentName { get; } = segmentName;
    public string FullNamespace { get; } = fullNamespace;
    public List<string> DirectFiles { get; } = new();
    public long DirectSize { get; set; }
    public Dictionary<string, ProjectSplittingNamespaceNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
    public ProjectSplittingNamespaceNode? Parent { get; set; }
}

internal sealed class ProjectSplittingBucket(ProjectSplittingNamespaceNode node, List<string> filePaths, long size)
{
    public ProjectSplittingNamespaceNode Node { get; } = node;
    public List<string> FilePaths { get; } = filePaths;
    public long Size { get; } = size;
}
