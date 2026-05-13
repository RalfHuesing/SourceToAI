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
    private static T UnwrapOrThrowValidation<T>(ExtractionResult<T> result)
    {
        if (!result.IsSuccess)
            throw new SourceToAiValidationException(result.ErrorMessage ?? "Validierung fehlgeschlagen.");
        return result.Value!;
    }

    private class ExportState { public bool Initialized; }

    /// <summary>
    /// Exportiert nacheinander jede Quellwurzel unter <paramref name="exportPath"/>.
    /// AST-/Parse-Cache: pro Export setzt <see cref="IMultiViewExportService.WriteMergedSolutionViews"/> (über den Loader) den Cache zurück.
    /// </summary>
    public async Task RunAsync(IEnumerable<string> rootPaths, string exportPath)
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

        var state = new ExportState();
        for (var i = 0; i < roots.Length; i++)
        {
            var rootPath = roots[i];
            var ordinal = i + 1;
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"[INFO] Quelle {ordinal}/{roots.Length}: {rootPath}");
            Console.WriteLine("--------------------------------------------------\n");

            await RunSingleSourceAsync(rootPath, exportPath, state);
        }

        Console.WriteLine("\n==================================================");
        Console.WriteLine($"- Alle Quellen verarbeitet ({roots.Length}).");
        Console.WriteLine("==================================================");
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

    private async Task RunSingleSourceAsync(string rootPath, string exportPath, ExportState state)
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
            }

            var plannedSolutionExportRoot = Path.GetFullPath(MultiViewExportPaths.GetSolutionExportRoot(exportPath, assemblyBaseName));

            var decompileDir = Path.Combine(plannedSolutionExportRoot, "decompile");
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
            }

            outputDir = MultiViewExportPaths.GetSolutionExportRoot(exportPath, solutionName);
        }

        var repositoryFolderName = GetRepositoryFolderNameForReadme(rootPath, effectiveRoot);

        var generatedAt = DateTimeOffset.UtcNow;
        var exportSessionId = Guid.NewGuid();
        try
        {
            var readme = readmeMarkdownGenerator.Generate(repositoryFolderName, generatedAt);
            File.WriteAllText(Path.Combine(exportPath, "readme.md"), readme);
            Console.WriteLine($"[INFO] readme.md → {exportPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] readme.md konnte nicht geschrieben werden: {ex.Message}");
        }

        try
        {
            var depGraphResult = dependencyGraphMarkdownGenerator.Generate(effectiveRoot, projects);
            if (depGraphResult.IsSuccess)
            {
                Directory.CreateDirectory(outputDir);
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
            Console.WriteLine("   -> Keine Solution-Docs gefunden (Root/.cursor …).\n");
        }

        var projectsWithFiles = new List<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)>();
        foreach (var project in projects)
        {
            var filesResult = fileDiscovery.FindFilesForProject(project, settings);
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

        Console.WriteLine();
        multiViewExportService.WriteMergedSolutionViews(
            exportPath,
            solutionName,
            effectiveRoot,
            exportSessionId,
            generatedAt,
            projectsWithFiles,
            solutionDocPaths);

        var successCount = projectsWithFiles.Count;
        Console.WriteLine("[INFO] Multi-View-Export (complete, signatures-only, public-only, dto-only) abgeschlossen.");

        Console.WriteLine("\n==================================================");
        Console.WriteLine($"- Fertig! {successCount} von {projects.Count} Projekten mit exportierbaren Dateien.");
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
