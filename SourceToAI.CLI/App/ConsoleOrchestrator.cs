using SourceToAI.CLI.App.Exceptions;
using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Decompilation;
using SourceToAI.CLI.Services.Discovery;
using SourceToAI.CLI.Services.Export;
using SourceToAI.CLI.Services.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SourceToAI.CLI.App;

public class ConsoleOrchestrator(
    ISolutionDiscoveryService solutionDiscovery,
    IFileDiscoveryService fileDiscovery,
    IAssemblyDecompilerService assemblyDecompiler,
    IDependencyGraphMarkdownGenerator dependencyGraphMarkdownGenerator,
    IMultiViewExportService multiViewExportService,
    IMultiViewReadmeMarkdownGenerator readmeMarkdownGenerator,
    AppSettings settings,
    IEnumerable<IPostExportTask> postExportTasks)
{
    /// <param name="AssemblyPath">Vollständiger Pfad zur .dll/.exe-Quelle.</param>
    /// <param name="DetailMessage">Mehrzeilige Fehlerdetails (z. B. entflachte AggregateException).</param>
    private readonly record struct AssemblySourceFailure(string AssemblyPath, string DetailMessage);

    private static T UnwrapOrThrowValidation<T>(ExtractionResult<T> result)
    {
        if (!result.IsSuccess)
            throw new SourceToAiValidationException(result.ErrorMessage ?? "Validierung fehlgeschlagen.");
        return result.Value!;
    }

    private class ExportState { public bool Initialized; }

    /// <summary>
    /// Baut eine mehrzeilige Beschreibung für fehlgeschlagene Assembly-Verarbeitung (insb. <see cref="AggregateException"/>).
    /// </summary>
    private static string BuildAssemblyProcessingFailureDetail(Exception ex)
    {
        if (ex is AggregateException aggregate)
        {
            var parts = aggregate.Flatten().InnerExceptions.Select(static e => e.Message).ToArray();
            return parts.Length > 0 ? string.Join(Environment.NewLine, parts) : aggregate.Message;
        }

        var lines = new List<string> { ex.Message };
        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
            lines.Add(inner.Message);

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Exportiert nacheinander jede Quellwurzel unter <paramref name="exportPath"/>.
    /// AST-/Parse-Cache: pro Export setzt <see cref="IMultiViewExportService.WriteMergedSolutionViews"/> (über den Loader) den Cache zurück.
    /// </summary>
    /// <returns><see langword="true"/>, wenn keine Assembly-Quelle wegen Dekompilierung/Discovery fehlgeschlagen ist; sonst <see langword="false"/>.</returns>
    public async Task<bool> RunAsync(IEnumerable<string> rootPaths, string exportPath)
    {
        var roots = rootPaths
            .Select(static p => p?.Trim())
            .Where(static p => !string.IsNullOrEmpty(p))
            .Select(static p => p!)
            .ToArray();

        if (roots.Length == 0)
            throw new SourceToAiValidationException("Mindestens ein gültiger Quellpfad ist erforderlich.");

        Console.WriteLine("==================================================");
        Console.WriteLine("🚀 SourceToAI - Standalone AI Feed Generator");
        Console.WriteLine("==================================================\n");
        Console.WriteLine($"[INFO] {roots.Length} Quelle(n), Export-Ziel: {exportPath}\n");

        var assemblyFailures = new List<AssemblySourceFailure>();
        var state = new ExportState();
        for (var i = 0; i < roots.Length; i++)
        {
            var rootPath = roots[i];
            var ordinal = i + 1;
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"[INFO] Quelle {ordinal}/{roots.Length}: {rootPath}");
            Console.WriteLine("--------------------------------------------------\n");

            await RunSingleSourceAsync(rootPath, exportPath, state, assemblyFailures);
        }

        Console.WriteLine("\n==================================================");
        Console.WriteLine($"- Alle Quellen verarbeitet ({roots.Length}).");
        Console.WriteLine("==================================================");

        if (assemblyFailures.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("==================================================");
            Console.WriteLine($"Zusammenfassung: fehlgeschlagene Assembly-Quellen ({assemblyFailures.Count})");
            Console.WriteLine("==================================================");
            for (var i = 0; i < assemblyFailures.Count; i++)
            {
                var f = assemblyFailures[i];
                Console.WriteLine($"{i + 1}) {f.AssemblyPath}");
                foreach (var line in f.DetailMessage.Split(
                             ['\r', '\n'],
                             StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    Console.WriteLine($"   {line}");
                }
            }

            Console.WriteLine("==================================================");
        }

        return assemblyFailures.Count == 0;
    }

    private static bool IsNetAssemblyFile(string path) =>
        !string.IsNullOrWhiteSpace(path)
        && File.Exists(path)
        && (Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Für Readme/Anzeige: bei reiner <c>decompile</c>-Blatt-Root den übergeordneten Ordnernamen (Assembly-/Export-Ebene).
    /// </summary>
    private static string GetRepositoryFolderNameForReadme(string userSourcePath, string effectiveRootPath)
    {
        var effectiveDir = new DirectoryInfo(Path.TrimEndingDirectorySeparator(effectiveRootPath));
        if (string.Equals(effectiveDir.Name, "decompile", StringComparison.OrdinalIgnoreCase))
        {
            var parent = effectiveDir.Parent;
            var fromParent = parent != null ? parent.Name : effectiveDir.Name;
            return string.IsNullOrEmpty(fromParent) ? effectiveDir.Name : fromParent;
        }

        return new DirectoryInfo(Path.TrimEndingDirectorySeparator(userSourcePath)).Name;
    }

    private static void PrepareGlobalExportRootDirectory(string exportPath)
    {
        var markerPath = Path.Combine(exportPath, MultiViewExportPaths.SafetyMarkerFileName);

        if (Directory.Exists(exportPath) && !File.Exists(markerPath))
        {
            throw new SourceToAiValidationException(
                $"Sicherheitsabbruch: Das Zielverzeichnis '{exportPath}' existiert bereits, enthält aber keine Sicherheits-Markerdatei ({MultiViewExportPaths.SafetyMarkerFileName}). Um Datenverlust zu vermeiden, wurde die Operation abgebrochen. "
                + $"Zum Fortfahren: Entweder den Ordner einmal manuell löschen oder — nur wenn Sie sicher sind, dass dies der richtige Exportordner ist — eine Datei „{MultiViewExportPaths.SafetyMarkerFileName}“ anlegen und den Befehl erneut ausführen.");
        }

        try
        {
            if (Directory.Exists(exportPath))
            {
                Console.WriteLine($"[INFO] Räume Ausgabeordner vollständig auf: {exportPath}");
                Directory.Delete(exportPath, recursive: true);
            }

            Directory.CreateDirectory(exportPath);
            File.WriteAllText(
                markerPath,
                "Generated by SourceToAI. Do not remove this file or the tool will refuse to delete this directory for safety reasons.");

            Directory.CreateDirectory(Path.Combine(exportPath, MultiViewExportPaths.IsolatedFolderName));
            Directory.CreateDirectory(Path.Combine(exportPath, MultiViewExportPaths.MergedFolderName));

            Console.WriteLine($"[INFO] Ausgabeordner bereit: {exportPath}\n");
        }
        catch (Exception ex)
        {
            if (ex is SourceToAiValidationException)
                throw;

            throw new SourceToAiValidationException(
                $"Konnte Ausgabeordner nicht vorbereiten: {ex.Message}",
                ex);
        }
    }

    private void TryWriteGlobalExportReadme(string exportPath)
    {
        try
        {
            var readme = readmeMarkdownGenerator.GenerateGlobalExportOverview(DateTimeOffset.UtcNow);
            File.WriteAllText(Path.Combine(exportPath, "readme.md"), readme);
            Console.WriteLine($"[INFO] readme.md (global) → {exportPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] readme.md (global) konnte nicht geschrieben werden: {ex.Message}");
        }
    }

    private async Task RunSingleSourceAsync(
        string rootPath,
        string exportPath,
        ExportState state,
        List<AssemblySourceFailure> assemblyFailures)
    {
        string effectiveRoot;
        string solutionName;
        List<ProjectDefinition> projects;
        string outputDir;

        if (IsNetAssemblyFile(rootPath))
        {
            var assemblyPath = Path.GetFullPath(rootPath);
            var assemblyBaseName = Path.GetFileNameWithoutExtension(assemblyPath);

            if (!state.Initialized)
            {
                PrepareGlobalExportRootDirectory(exportPath);
                state.Initialized = true;
                TryWriteGlobalExportReadme(exportPath);
            }

            var plannedSolutionExportRoot = Path.GetFullPath(MultiViewExportPaths.GetSolutionExportRoot(exportPath, assemblyBaseName));

            var decompileDir = Path.Combine(plannedSolutionExportRoot, "decompile");
            try
            {
                // RunAsync/CLI reichen das Abbruchtoken noch nicht durch — sobald verfügbar hier an den Decompiler durchreichen.
                effectiveRoot = assemblyDecompiler.DecompileToProjectDirectory(
                    assemblyPath,
                    decompileDir,
                    CancellationToken.None);

                solutionName = UnwrapOrThrowValidation(solutionDiscovery.GetSolutionName(effectiveRoot));
                if (!string.Equals(solutionName, assemblyBaseName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new SourceToAiValidationException(
                        $"Ermittelter Solution-Name „{solutionName}“ weicht vom Assembly-Basisnamen „{assemblyBaseName}“ ab (Export-Pfad-Invariante).");
                }

                var exportRootFromName = Path.GetFullPath(MultiViewExportPaths.GetSolutionExportRoot(exportPath, solutionName));
                if (!string.Equals(exportRootFromName, plannedSolutionExportRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new SourceToAiValidationException(
                        $"Export-Wurzel „{exportRootFromName}“ entspricht nicht der erwarteten Assembly-Export-Wurzel „{plannedSolutionExportRoot}“.");
                }

                Console.WriteLine($"[INFO] Solution erkannt: {solutionName}");
                projects = UnwrapOrThrowValidation(solutionDiscovery.FindProjects(effectiveRoot));
                Console.WriteLine($"[INFO] {projects.Count} Projekte gefunden.\n");
                outputDir = plannedSolutionExportRoot;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var detail = BuildAssemblyProcessingFailureDetail(ex);
                assemblyFailures.Add(new AssemblySourceFailure(assemblyPath, detail));
                Console.WriteLine($"[WARN] Assembly-Quelle übersprungen ({assemblyPath}): {ex.Message}");
                return;
            }
        }
        else
        {
            effectiveRoot = rootPath;
            solutionName = UnwrapOrThrowValidation(solutionDiscovery.GetSolutionName(effectiveRoot));
            Console.WriteLine($"[INFO] Solution erkannt: {solutionName}");

            projects = UnwrapOrThrowValidation(solutionDiscovery.FindProjects(effectiveRoot));
            Console.WriteLine($"[INFO] {projects.Count} Projekte gefunden.\n");

            if (!state.Initialized)
            {
                PrepareGlobalExportRootDirectory(exportPath);
                state.Initialized = true;
                TryWriteGlobalExportReadme(exportPath);
            }

            outputDir = MultiViewExportPaths.GetSolutionExportRoot(exportPath, solutionName);
        }

        var repositoryFolderName = GetRepositoryFolderNameForReadme(rootPath, effectiveRoot);

        var generatedAt = DateTimeOffset.UtcNow;
        var exportSessionId = Guid.NewGuid();
        try
        {
            Directory.CreateDirectory(outputDir);
            var readme = readmeMarkdownGenerator.GenerateIsolatedSolutionReadme(
                solutionName,
                repositoryFolderName,
                generatedAt);
            File.WriteAllText(Path.Combine(outputDir, "readme.md"), readme);
            Console.WriteLine($"[INFO] readme.md (Lösung) → {outputDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] readme.md (Lösung) konnte nicht geschrieben werden: {ex.Message}");
        }

        try
        {
            var depGraphResult = dependencyGraphMarkdownGenerator.Generate(effectiveRoot, projects);
            if (depGraphResult.IsSuccess)
            {
                File.WriteAllText(Path.Combine(outputDir, "dependency-graph.md"), depGraphResult.Value!);
                Console.WriteLine($"[INFO] dependency-graph.md → {outputDir}");
            }
            else
            {
                Console.WriteLine($"[WARN] dependency-graph.md: {depGraphResult.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] dependency-graph.md konnte nicht geschrieben werden: {ex.Message}");
        }

        IReadOnlyList<string>? solutionDocPaths = null;
        var docsResult = fileDiscovery.FindSolutionDocs(effectiveRoot, settings);
        if (docsResult.IsSuccess && docsResult.Value!.Count > 0)
        {
            solutionDocPaths = docsResult.Value;
            Console.WriteLine(
                $"[INFO] Solution-Dokumentation: {solutionDocPaths.Count} Datei(en) → eigene Datei unter complete/ (Projekt „.Docs“).\n");
        }
        else
        {
            Console.WriteLine("   -> Keine Solution-Docs gefunden (Root-README, Docs/, .cursor, .github …).\n");
        }

        var projectsWithFiles = new List<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)>();
        foreach (var project in projects)
        {
            var filesResult = fileDiscovery.FindFilesForProject(project, effectiveRoot, settings);
            if (!filesResult.IsSuccess)
            {
                Console.WriteLine($"   -> [FEHLER] {project.ProjectName}: {filesResult.ErrorMessage}");
                continue;
            }

            if (filesResult.Warnings is { Count: > 0 } scanWarnings)
            {
                foreach (var line in scanWarnings)
                {
                    Console.WriteLine($"   -> [WARN] {project.ProjectName}: {line}");
                }
            }

            if (filesResult.Value!.Count == 0)
            {
                Console.WriteLine($"   -> Übersprungen (Keine relevanten Dateien): {project.ProjectName}");
                continue;
            }

            projectsWithFiles.Add((project, filesResult.Value));
            Console.WriteLine($"   -> Multi-View-Quellen: {project.ProjectName} ({filesResult.Value.Count} Dateien)");
        }

        var unmappedResult = fileDiscovery.FindUnmappedDirectories(effectiveRoot, projects, settings);
        IReadOnlyList<(string DirectoryName, IReadOnlyList<string> AbsoluteFilePaths)> unmappedForExport;
        var unmappedExportCount = 0;

        if (!unmappedResult.IsSuccess)
        {
            Console.WriteLine($"   -> [WARN] Unmapped-Verzeichnisse: {unmappedResult.ErrorMessage}");
            unmappedForExport = [];
        }
        else
        {
            var raw = unmappedResult.Value!;
            unmappedExportCount = raw.Count;
            unmappedForExport = raw
                .OrderBy(x => x.DirectoryName, StringComparer.OrdinalIgnoreCase)
                .Select(x => (x.DirectoryName, (IReadOnlyList<string>)x.AbsolutePaths))
                .ToList();

            foreach (var (name, paths) in unmappedForExport)
            {
                Console.WriteLine($"[INFO] Unmapped Directory found: {name} ({paths.Count} Dateien)");
            }

            if (unmappedResult.Warnings is { Count: > 0 } unmappedWarnings)
            {
                foreach (var line in unmappedWarnings)
                {
                    Console.WriteLine($"   -> [WARN] Unmapped-Verzeichnis-Scan: {line}");
                }
            }
        }

        Console.WriteLine();
        multiViewExportService.WriteMergedSolutionViews(
            exportPath,
            solutionName,
            effectiveRoot,
            exportSessionId,
            generatedAt,
            projectsWithFiles,
            solutionDocPaths,
            unmappedForExport);

        var successCount = projectsWithFiles.Count;
        Console.WriteLine("[INFO] Multi-View-Export (complete, signatures-only, public-only, dto-only) abgeschlossen.");

        Console.WriteLine("\n==================================================");
        var unmappedSummary = unmappedExportCount > 0
            ? $" Zusätzlich {unmappedExportCount} nicht zugeordnete(s) Verzeichnis(se) mit Dateien."
            : string.Empty;
        Console.WriteLine($"- Fertig! {successCount} von {projects.Count} Projekten mit exportierbaren Dateien.{unmappedSummary}");
        Console.WriteLine($"- Ausgabe (Lösung): {outputDir}");
        Console.WriteLine($"- Ausgabe (Global): {exportPath}");
        Console.WriteLine("==================================================");

        if (postExportTasks.Any())
        {
            Console.WriteLine("\n- Führe Post-Export Tasks aus...");
            foreach (var task in postExportTasks)
            {
                await task.ExecuteAsync(solutionName, outputDir);
            }
            Console.WriteLine("- Post-Export Tasks abgeschlossen.");
        }
    }
}
