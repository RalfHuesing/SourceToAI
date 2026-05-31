using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using SourceToAI.CLI.App.Exceptions;
using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Export.AiFeed;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.CLI.Services.Processing.Markdown;
namespace SourceToAI.CLI.Services.Export;

public sealed class MultiViewExportService(
    IEnumerable<IMarkdownProjectViewBuilder> viewBuilders,
    ICSharpDocumentLoader csharpDocumentLoader,
    IAiFeedMarkdownComposer markdownComposer,
    ProjectSplittingEngine splittingEngine,
    AppSettings appSettings) : IMultiViewExportService
{
    /// <summary>
    /// Obergrenze paralleler View-Builds (Roslyn/Rewrite + Compose) pro Exportlauf — siehe Task 03 / Projektrichtlinien.
    /// </summary>
    private const int MaxConcurrentViewBuilds = 5;

    private static readonly string[] ViewKeyOrder = ["complete", "signatures-only", "public-only", "dto-only"];

    public void WriteMergedSolutionViews(
        string outputRoot,
        string solutionDisplayName,
        string solutionRootPath,
        Guid sessionId,
        DateTimeOffset generated,
        IReadOnlyList<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)> projectsWithFiles,
        IReadOnlyList<string>? solutionDocumentationAbsolutePaths,
        IReadOnlyList<(string DirectoryName, IReadOnlyList<string> AbsoluteFilePaths)> unmappedDirectories)
    {
        csharpDocumentLoader.Clear();

        var buildersByKey = viewBuilders.ToDictionary(b => b.ViewKey, StringComparer.Ordinal);
        var usedStemsPerView = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var vk in ViewKeyOrder)
            usedStemsPerView[vk] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var orderedProjects = projectsWithFiles
            .OrderBy(p => p.Project.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var exportUnits = new List<(ProjectDefinition Project, IReadOnlyList<string> Paths, bool DocsOnlyInCompleteView)>();
        if (solutionDocumentationAbsolutePaths is { Count: > 0 })
        {
            var docProject = new ProjectDefinition(".Docs", Path.Combine(solutionRootPath, "virtual.csproj"));
            exportUnits.Add((docProject, solutionDocumentationAbsolutePaths, true));
        }

        var isSplittingActive = appSettings.MaxFileSizeKb > 0 && appSettings.MaxFileCount > 0;
        var virtualProjectSplitInfo = new Dictionary<string, (string RealProj, string SubNamespace)>(StringComparer.OrdinalIgnoreCase);

        foreach (var (project, paths) in orderedProjects)
        {
            if (paths.Count == 0)
                continue;

            if (isSplittingActive)
            {
                var partitions = splittingEngine.PartitionProject(
                    project,
                    paths,
                    appSettings.MaxFileSizeKb,
                    appSettings.MaxFileCount,
                    appSettings.SuppressCorePartition);
                foreach (var partition in partitions)
                {
                    var partitionName = partition.SubNamespaceName;
                    string virtualProjName;

                    if (string.IsNullOrEmpty(partitionName))
                    {
                        virtualProjName = project.ProjectName;
                    }
                    else
                    {
                        var prefix = project.ProjectName + ".";
                        if (partitionName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            var sub = partitionName.Substring(prefix.Length);
                            virtualProjName = string.IsNullOrEmpty(sub) ? project.ProjectName : $"{project.ProjectName}.{sub}";
                        }
                        else if (string.Equals(partitionName, project.ProjectName, StringComparison.OrdinalIgnoreCase))
                        {
                            virtualProjName = project.ProjectName;
                        }
                        else
                        {
                            virtualProjName = $"{project.ProjectName}.{partitionName}";
                        }
                    }

                    var virtualCsproj = Path.Combine(project.RootDirectory, $"{virtualProjName}.virtual.csproj");
                    var virtualProject = new ProjectDefinition(virtualProjName, virtualCsproj);
                    virtualProjectSplitInfo[virtualProjName] = (project.ProjectName, partitionName);

                    exportUnits.Add((virtualProject, partition.Paths, false));
                }
            }
            else
            {
                exportUnits.Add((project, paths, false));
            }
        }

        foreach (var (directoryName, paths) in unmappedDirectories
                     .Where(u => u.AbsoluteFilePaths.Count > 0)
                     .OrderBy(u => u.DirectoryName, StringComparer.OrdinalIgnoreCase))
        {
            var virtualCsproj = Path.Combine(solutionRootPath, directoryName, "virtual.csproj");
            var virtualProject = new ProjectDefinition(directoryName, virtualCsproj);
            exportUnits.Add((virtualProject, paths, true));
        }

        var workSlots = new List<ViewWorkSlot>();
        foreach (var (project, paths, docsOnlyInCompleteView) in exportUnits)
        {
            foreach (var viewKey in ViewKeyOrder)
            {
                if (docsOnlyInCompleteView && !viewKey.Equals("complete", StringComparison.Ordinal))
                    continue;

                if (!buildersByKey.TryGetValue(viewKey, out var builder))
                {
                    throw new SourceToAiExportException(
                        $"Kein Markdown-View-Builder fuer \"{viewKey}\" registriert.");
                }

                workSlots.Add(new ViewWorkSlot(viewKey, builder, project, paths));
            }
        }

        var parallelErrors = new ConcurrentQueue<Exception>();
        var composedBodies = new string?[workSlots.Count];
        RunBoundedParallel(
            MaxConcurrentViewBuilds,
            workSlots.Count,
            i =>
            {
                var slot = workSlots[i];
                var part = slot.Builder.BuildContentSegments(slot.Project, slot.Paths);
                if (!part.IsSuccess)
                {
                    parallelErrors.Enqueue(
                        new SourceToAiExportException(part.ErrorMessage ?? "View-Build fehlgeschlagen."));
                    return;
                }

                if (part.Warnings is { Count: > 0 } buildWarnings)
                {
                    foreach (var line in buildWarnings)
                    {
                        Console.WriteLine(
                            $"   -> [WARN] {slot.Project.ProjectName} ({slot.ViewKey}): {line}");
                    }
                }

                if (part.Value!.Count == 0)
                {
                    composedBodies[i] = null;
                    return;
                }

                var body = markdownComposer.Compose(
                    solutionDisplayName,
                    slot.Project.ProjectName,
                    sessionId,
                    generated,
                    part.Value!);
                composedBodies[i] = body;
            },
            parallelErrors);

        if (!parallelErrors.IsEmpty)
        {
            throw new SourceToAiExportException(
                "Multi-View-Export: Mindestens ein View-Build ist fehlgeschlagen.",
                new AggregateException(parallelErrors.ToArray()));
        }

        for (var i = 0; i < workSlots.Count; i++)
        {
            var body = composedBodies[i];
            if (body is null)
                continue;

            var slot = workSlots[i];
            var viewFolder = MultiViewExportPaths.GetViewFolderNameForViewKey(slot.ViewKey);
            var usedStems = usedStemsPerView[slot.ViewKey];

            string stemName;
            if (virtualProjectSplitInfo.TryGetValue(slot.Project.ProjectName, out var splitInfo))
            {
                string subNamespaceSuffix = string.Empty;
                if (!string.IsNullOrEmpty(splitInfo.SubNamespace))
                {
                    var prefix = splitInfo.RealProj + ".";
                    if (splitInfo.SubNamespace.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        subNamespaceSuffix = splitInfo.SubNamespace.Substring(prefix.Length);
                    }
                    else if (string.Equals(splitInfo.SubNamespace, splitInfo.RealProj, StringComparison.OrdinalIgnoreCase))
                    {
                        subNamespaceSuffix = string.Empty;
                    }
                    else
                    {
                        subNamespaceSuffix = splitInfo.SubNamespace;
                    }
                }

                var projectDisplayName = string.IsNullOrEmpty(subNamespaceSuffix)
                    ? splitInfo.RealProj
                    : $"{splitInfo.RealProj}_{subNamespaceSuffix}";

                stemName = MultiViewExportPaths.BuildSanitizedExportFileStem(solutionDisplayName, projectDisplayName, slot.ViewKey);
            }
            else
            {
                stemName = MultiViewExportPaths.BuildSanitizedExportFileStem(solutionDisplayName, slot.Project.ProjectName, slot.ViewKey);
            }

            var stem = MultiViewExportPaths.AllocateUniqueFileStem(stemName, usedStems);
            WriteProjectViewFiles(outputRoot, solutionDisplayName, viewFolder, stem, body);
        }
    }

    private static void RunBoundedParallel(int maxConcurrency, int workCount, Action<int> work, ConcurrentQueue<Exception> errors)
    {
        if (workCount <= 0)
            return;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Clamp(maxConcurrency, 1, int.MaxValue)
        };

        Parallel.For(0, workCount, options, i =>
        {
            try
            {
                work(i);
            }
            catch (Exception ex)
            {
                errors.Enqueue(ex);
            }
        });
    }

    private sealed record ViewWorkSlot(
        string ViewKey,
        IMarkdownProjectViewBuilder Builder,
        ProjectDefinition Project,
        IReadOnlyList<string> Paths);

    private static void WriteProjectViewFiles(string outputRoot, string solutionDisplayName, string viewFolder, string uniqueStem, string body)
    {
        var isolatedRoot = MultiViewExportPaths.GetSolutionExportRoot(outputRoot, solutionDisplayName);
        var mergedRoot = Path.Combine(outputRoot, MultiViewExportPaths.MergedFolderName);

        var isolatedOutPath = MultiViewExportPaths.GetViewOutputPath(isolatedRoot, viewFolder, uniqueStem);
        var mergedOutPath = MultiViewExportPaths.GetViewOutputPath(mergedRoot, viewFolder, uniqueStem);

        WriteTextEnsuringDirectory(isolatedOutPath, body);
        WriteTextEnsuringDirectory(mergedOutPath, body);
    }

    private static void WriteTextEnsuringDirectory(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, content);
    }
}
