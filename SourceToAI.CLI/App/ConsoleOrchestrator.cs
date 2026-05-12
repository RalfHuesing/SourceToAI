using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Discovery;
using SourceToAI.CLI.Services.Export;
using SourceToAI.CLI.Services.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SourceToAI.CLI.App;

public class ConsoleOrchestrator(
    ISolutionDiscoveryService solutionDiscovery,
    IFileDiscoveryService fileDiscovery,
    IDependencyGraphMarkdownGenerator dependencyGraphMarkdownGenerator,
    IMultiViewExportService multiViewExportService,
    IMultiViewReadmeMarkdownGenerator readmeMarkdownGenerator,
    AppSettings settings,
    IEnumerable<IPostExportTask> postExportTasks)
{
    public async Task RunAsync(string rootPath, string exportPath)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("🚀 SourceToAI - Standalone AI Feed Generator");
        Console.WriteLine("==================================================\n");

        var solutionResult = solutionDiscovery.GetSolutionName(rootPath);
        if (!solutionResult.IsSuccess)
        {
            Console.WriteLine($"[FEHLER] {solutionResult.ErrorMessage}");
            return;
        }
        var solutionName = solutionResult.Value!;
        Console.WriteLine($"[INFO] Solution erkannt: {solutionName}");

        var projectsResult = solutionDiscovery.FindProjects(rootPath);
        if (!projectsResult.IsSuccess)
        {
            Console.WriteLine($"[FEHLER] {projectsResult.ErrorMessage}");
            return;
        }
        var projects = projectsResult.Value!;
        Console.WriteLine($"[INFO] {projects.Count} Projekte gefunden.\n");

        var outputDir = MultiViewExportPaths.GetSolutionExportRoot(exportPath, solutionName);
        var repositoryFolderName = new DirectoryInfo(Path.TrimEndingDirectorySeparator(rootPath)).Name;

        try
        {
            if (Directory.Exists(outputDir))
            {
                Console.WriteLine($"[INFO] Räume Ausgabeordner vollständig auf: {outputDir}");
                Directory.Delete(outputDir, recursive: true);
            }
            Directory.CreateDirectory(outputDir);
            Console.WriteLine($"[INFO] Ausgabeordner bereit: {outputDir}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FEHLER] Konnte Ausgabeordner nicht vorbereiten: {ex.Message}");
            return;
        }

        var generatedAt = DateTimeOffset.UtcNow;
        try
        {
            var readme = readmeMarkdownGenerator.Generate(repositoryFolderName, generatedAt);
            File.WriteAllText(Path.Combine(outputDir, "readme.md"), readme);
            Console.WriteLine($"[INFO] readme.md → {outputDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] readme.md konnte nicht geschrieben werden: {ex.Message}");
        }

        try
        {
            var depGraphResult = dependencyGraphMarkdownGenerator.Generate(rootPath, projects);
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
        var docsResult = fileDiscovery.FindSolutionDocs(rootPath, settings);
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

            if (filesResult.Value!.Count == 0)
            {
                Console.WriteLine($"   -> Übersprungen (Keine relevanten Dateien): {project.ProjectName}");
                continue;
            }

            projectsWithFiles.Add((project, filesResult.Value));
            Console.WriteLine($"   -> Multi-View-Quellen: {project.ProjectName} ({filesResult.Value.Count} Dateien)");
        }

        Console.WriteLine();
        var multiViewResult = multiViewExportService.WriteMergedSolutionViews(
            outputDir,
            solutionName,
            rootPath,
            projectsWithFiles,
            solutionDocPaths);

        var successCount = projectsWithFiles.Count;
        if (!multiViewResult.IsSuccess)
        {
            Console.WriteLine($"[FEHLER] Multi-View-Export: {multiViewResult.ErrorMessage}");
            successCount = 0;
        }
        else
        {
            Console.WriteLine("[INFO] Multi-View-Export (complete, signatures-only, public-only, dto-only) abgeschlossen.");
        }

        Console.WriteLine("\n==================================================");
        Console.WriteLine($"- Fertig! {successCount} von {projects.Count} Projekten mit exportierbaren Dateien.");
        Console.WriteLine($"- Ausgabe: {outputDir}");
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
