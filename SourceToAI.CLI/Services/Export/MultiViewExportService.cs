using System.Collections.Concurrent;
using System.Linq;
using SourceToAI.CLI.App.Exceptions;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Export.AiFeed;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.CLI.Services.Processing.Markdown;
namespace SourceToAI.CLI.Services.Export;

public sealed class MultiViewExportService(
    IEnumerable<IMarkdownProjectViewBuilder> viewBuilders,
    ICSharpDocumentLoader csharpDocumentLoader,
    IAiFeedMarkdownComposer markdownComposer) : IMultiViewExportService
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
        IReadOnlyList<string>? solutionDocumentationAbsolutePaths)
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

        foreach (var (project, paths) in orderedProjects)
        {
            if (paths.Count == 0)
                continue;
            exportUnits.Add((project, paths, false));
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
                        $"Kein Markdown-View-Builder für „{viewKey}“ registriert.");
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

            var stem = MultiViewExportPaths.AllocateUniqueFileStem(
                MultiViewExportPaths.BuildSanitizedExportFileStem(solutionDisplayName, slot.Project.ProjectName),
                usedStems);
            WriteProjectViewFile(outputRoot, viewFolder, stem, body);
        }
    }

    private static void RunBoundedParallel(int maxConcurrency, int workCount, Action<int> work, ConcurrentQueue<Exception> errors)
    {
        if (workCount <= 0)
            return;

        var degree = Math.Clamp(maxConcurrency, 1, int.MaxValue);
        using var semaphore = new SemaphoreSlim(degree);
        var tasks = new Task[workCount];
        for (var i = 0; i < workCount; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                semaphore.Wait();
                try
                {
                    work(index);
                }
                catch (Exception ex)
                {
                    errors.Enqueue(ex);
                }
                finally
                {
                    _ = semaphore.Release();
                }
            });
        }

        Task.WaitAll(tasks);
    }

    private sealed record ViewWorkSlot(
        string ViewKey,
        IMarkdownProjectViewBuilder Builder,
        ProjectDefinition Project,
        IReadOnlyList<string> Paths);

    private static void WriteProjectViewFile(string outputRoot, string viewFolder, string uniqueStem, string body)
    {
        var outPath = MultiViewExportPaths.GetViewOutputPath(outputRoot, viewFolder, uniqueStem);
        var outDir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrEmpty(outDir))
            Directory.CreateDirectory(outDir);

        File.WriteAllText(outPath, body);
    }
}
