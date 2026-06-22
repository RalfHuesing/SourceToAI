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
    private const int MaxConcurrentViewBuilds = 5;

    private static readonly string[] ViewKeyOrder = ["complete", "signatures-only", "public-only", "dto-only"];

    private record ExportUnit(ProjectDefinition Project, IReadOnlyList<string> Paths, bool DocsOnlyInCompleteView);

    private record ExportUnitsResult(
        List<ExportUnit> ExportUnits,
        Dictionary<string, (string RealProj, string SubNamespace)> VirtualProjectSplitInfo);

    private record OutputWriteParams(
        string OutputRoot,
        string SolutionDisplayName,
        Dictionary<string, (string RealProj, string SubNamespace)> VirtualProjectSplitInfo);

    public void WriteMergedSolutionViews(SolutionViewExportArgs args)
    {
        csharpDocumentLoader.Clear();

        var buildersByKey = viewBuilders.ToDictionary(b => b.ViewKey, StringComparer.Ordinal);

        var unitsResult = BuildExportUnits(args.SolutionRootPath, args.ProjectsWithFiles, args.SolutionDocumentationAbsolutePaths, args.UnmappedDirectories);
        var workSlots = BuildWorkSlots(unitsResult.ExportUnits, buildersByKey);
        var composedBodies = BuildComposedBodiesParallel(workSlots, args.SolutionDisplayName, args.SessionId, args.Generated);

        var wp = new OutputWriteParams(args.OutputRoot, args.SolutionDisplayName, unitsResult.VirtualProjectSplitInfo);
        WriteOutputFiles(wp, workSlots, composedBodies);
    }

    private ExportUnitsResult BuildExportUnits(
        string solutionRootPath,
        IReadOnlyList<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)> projectsWithFiles,
        IReadOnlyList<string>? solutionDocs,
        IReadOnlyList<(string DirectoryName, IReadOnlyList<string> AbsoluteFilePaths)> unmappedDirs)
    {
        var exportUnits = new List<ExportUnit>();
        var virtualProjectSplitInfo = new Dictionary<string, (string RealProj, string SubNamespace)>(StringComparer.OrdinalIgnoreCase);

        if (solutionDocs is { Count: > 0 })
        {
            var docProject = new ProjectDefinition(".Docs", Path.Combine(solutionRootPath, "virtual.csproj"));
            exportUnits.Add(new ExportUnit(docProject, solutionDocs, true));
        }

        var isSplittingActive = appSettings.MaxFileSizeKb > 0 && appSettings.MaxFileCount > 0;
        var orderedProjects = projectsWithFiles
            .OrderBy(p => p.Project.ProjectName, StringComparer.OrdinalIgnoreCase);

        foreach (var (project, paths) in orderedProjects)
        {
            if (paths.Count == 0)
                continue;

            if (isSplittingActive)
            {
                var partitions = splittingEngine.PartitionProject(
                    project,
                    paths,
                    new ProjectSplittingOptions(appSettings.MaxFileSizeKb, appSettings.MaxFileCount, appSettings.SuppressCorePartition));
                foreach (var partition in partitions)
                {
                    var partitionName = partition.SubNamespaceName;
                    var virtualProjName = GetVirtualProjectName(project.ProjectName, partitionName);
                    var virtualCsproj = Path.Combine(project.RootDirectory, $"{virtualProjName}.virtual.csproj");
                    var virtualProject = new ProjectDefinition(virtualProjName, virtualCsproj);
                    virtualProjectSplitInfo[virtualProjName] = (project.ProjectName, partitionName);

                    exportUnits.Add(new ExportUnit(virtualProject, partition.Paths, false));
                }
            }
            else
            {
                exportUnits.Add(new ExportUnit(project, paths, false));
            }
        }

        foreach (var (directoryName, paths) in unmappedDirs
                     .Where(u => u.AbsoluteFilePaths.Count > 0)
                     .OrderBy(u => u.DirectoryName, StringComparer.OrdinalIgnoreCase))
        {
            var virtualCsproj = Path.Combine(solutionRootPath, directoryName, "virtual.csproj");
            var virtualProject = new ProjectDefinition(directoryName, virtualCsproj);
            exportUnits.Add(new ExportUnit(virtualProject, paths, true));
        }

        return new ExportUnitsResult(exportUnits, virtualProjectSplitInfo);
    }

    private static string GetVirtualProjectName(string projectName, string partitionName)
    {
        if (string.IsNullOrEmpty(partitionName))
            return projectName;

        var prefix = projectName + ".";
        if (partitionName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var sub = partitionName.Substring(prefix.Length);
            return string.IsNullOrEmpty(sub) ? projectName : $"{projectName}.{sub}";
        }
        if (string.Equals(partitionName, projectName, StringComparison.OrdinalIgnoreCase))
            return projectName;

        return $"{projectName}.{partitionName}";
    }

    private static List<ViewWorkSlot> BuildWorkSlots(
        IReadOnlyList<ExportUnit> exportUnits,
        Dictionary<string, IMarkdownProjectViewBuilder> buildersByKey)
    {
        var workSlots = new List<ViewWorkSlot>();
        foreach (var unit in exportUnits)
        {
            foreach (var viewKey in ViewKeyOrder)
            {
                if (unit.DocsOnlyInCompleteView && !viewKey.Equals("complete", StringComparison.Ordinal))
                    continue;

                if (!buildersByKey.TryGetValue(viewKey, out var builder))
                {
                    throw new SourceToAiExportException(
                        $"Kein Markdown-View-Builder fuer \"{viewKey}\" registriert.");
                }

                workSlots.Add(new ViewWorkSlot(viewKey, builder, unit.Project, unit.Paths));
            }
        }
        return workSlots;
    }

    private string?[] BuildComposedBodiesParallel(
        IReadOnlyList<ViewWorkSlot> workSlots,
        string solutionDisplayName,
        Guid sessionId,
        DateTimeOffset generated)
    {
        var parallelErrors = new ConcurrentQueue<Exception>();
        var composedBodies = new string?[workSlots.Count];
        var sessionInfo = new AiFeedSessionInfo(solutionDisplayName, sessionId, generated);

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
                        Console.WriteLine($"   -> [WARN] {slot.Project.ProjectName} ({slot.ViewKey}): {line}");
                    }
                }

                if (part.Value!.Count == 0)
                {
                    composedBodies[i] = null;
                    return;
                }

                var body = markdownComposer.Compose(
                    sessionInfo,
                    slot.Project.ProjectName,
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

        return composedBodies;
    }

    private void WriteOutputFiles(
        OutputWriteParams wp,
        IReadOnlyList<ViewWorkSlot> workSlots,
        string?[] composedBodies)
    {
        var usedStemsPerView = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var vk in ViewKeyOrder)
            usedStemsPerView[vk] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < workSlots.Count; i++)
        {
            var body = composedBodies[i];
            if (body is null)
                continue;

            var slot = workSlots[i];
            var viewFolder = MultiViewExportPaths.GetViewFolderNameForViewKey(slot.ViewKey);
            var usedStems = usedStemsPerView[slot.ViewKey];
            var stemName = GetStemName(wp.SolutionDisplayName, slot.Project.ProjectName, slot.ViewKey, wp.VirtualProjectSplitInfo);
            var stem = MultiViewExportPaths.AllocateUniqueFileStem(stemName, usedStems);

            var isolatedRoot = MultiViewExportPaths.GetSolutionExportRoot(wp.OutputRoot, wp.SolutionDisplayName);
            var mergedRoot = Path.Combine(wp.OutputRoot, MultiViewExportPaths.MergedFolderName);
            var isolatedOutPath = MultiViewExportPaths.GetViewOutputPath(isolatedRoot, viewFolder, stem);
            var mergedOutPath = MultiViewExportPaths.GetViewOutputPath(mergedRoot, viewFolder, stem);

            WriteProjectViewFiles(isolatedOutPath, mergedOutPath, body);
        }
    }

    private static string GetStemName(
        string solutionDisplayName,
        string projectName,
        string viewKey,
        Dictionary<string, (string RealProj, string SubNamespace)> virtualProjectSplitInfo)
    {
        if (virtualProjectSplitInfo.TryGetValue(projectName, out var splitInfo))
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

            return MultiViewExportPaths.BuildSanitizedExportFileStem(solutionDisplayName, projectDisplayName, viewKey);
        }

        return MultiViewExportPaths.BuildSanitizedExportFileStem(solutionDisplayName, projectName, viewKey);
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

    private static void WriteProjectViewFiles(string isolatedPath, string mergedPath, string body)
    {
        WriteTextEnsuringDirectory(isolatedPath, body);
        WriteTextEnsuringDirectory(mergedPath, body);
    }

    private static void WriteTextEnsuringDirectory(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, content);
    }
}
