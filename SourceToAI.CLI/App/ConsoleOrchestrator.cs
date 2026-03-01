using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Discovery;
using SourceToAI.CLI.Services.Integration;
using SourceToAI.CLI.Services.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceToAI.CLI.App;

public class ConsoleOrchestrator(
    ISolutionDiscoveryService solutionDiscovery,
    IFileDiscoveryService fileDiscovery,
    IFeedGenerator feedGenerator,
    AppSettings settings,
    IEnumerable<IPostExportTask> postExportTasks)
{
    public async Task RunAsync(string rootPath, string exportPath)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("🚀 SourceToAI - Standalone AI Feed Generator");
        Console.WriteLine("==================================================\n");

        // 1. Solution Name ermitteln
        var solutionResult = solutionDiscovery.GetSolutionName(rootPath);
        if (!solutionResult.IsSuccess)
        {
            Console.WriteLine($"[FEHLER] {solutionResult.ErrorMessage}");
            return;
        }
        var solutionName = solutionResult.Value!;
        Console.WriteLine($"[INFO] Solution erkannt: {solutionName}");

        // 2. Projekte finden
        var projectsResult = solutionDiscovery.FindProjects(rootPath);
        if (!projectsResult.IsSuccess)
        {
            Console.WriteLine($"[FEHLER] {projectsResult.ErrorMessage}");
            return;
        }
        var projects = projectsResult.Value!;
        Console.WriteLine($"[INFO] {projects.Count} Projekte gefunden.\n");

        // 3. Ausgabe-Verzeichnis vorbereiten (Einmal pro Run)
        var dateSuffix = DateTime.Now.ToString("yyyyMMdd");
        var outputDir = Path.Combine(exportPath, solutionName);

        try
        {
            if (Directory.Exists(outputDir))
            {
                Console.WriteLine($"[INFO] Ausgabeordner '{solutionName}' existiert bereits. Lösche alte .md-Dateien...");
                // Sicherheitsfilter: Nur .md Dateien im Hauptverzeichnis (nicht rekursiv) löschen
                var existingMdFiles = Directory.GetFiles(outputDir, "*.md", SearchOption.TopDirectoryOnly);
                foreach (var file in existingMdFiles)
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(outputDir);
                Console.WriteLine($"[INFO] Ausgabeordner erstellt: {outputDir}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FEHLER] Konnte Ausgabeordner nicht erstellen: {ex.Message}");
            return;
        }

        Console.WriteLine($"- Verarbeite Solution-Dokumentation (Root & .cursor)...");
        var docsResult = fileDiscovery.FindSolutionDocs(rootPath, settings);
        int successCount = 0;

        if (docsResult.IsSuccess && docsResult.Value!.Count > 0)
        {
            // Wir faken ein Projekt. Der Pfad "virtual.csproj" sorgt dafür, dass RootDirectory == rootPath ist!
            var docProject = new ProjectDefinition(".Docs", Path.Combine(rootPath, "virtual.csproj"));
            var docFeedResult = feedGenerator.GenerateFeed(solutionName, docProject, docsResult.Value);

            if (docFeedResult.IsSuccess)
            {
                var docFileName = $"{solutionName}.Docs-{dateSuffix}.md";
                File.WriteAllText(Path.Combine(outputDir, docFileName), docFeedResult.Value!);
                Console.WriteLine($"   -> Gespeichert: {docFileName} ({docsResult.Value.Count} Dateien)\n");
                successCount++;
            }
            else
            {
                Console.WriteLine($"   -> [FEHLER] Generierung fehlgeschlagen: {docFeedResult.ErrorMessage}\n");
            }
        }
        else
        {
            Console.WriteLine($"   -> Übersprungen (Keine Solution-Docs gefunden).\n");
        }

        // 4. Projekte verarbeiten
        foreach (var project in projects)
        {
            Console.WriteLine($"- Verarbeite Projekt: {project.ProjectName}...");

            var filesResult = fileDiscovery.FindFilesForProject(project, settings);
            if (!filesResult.IsSuccess || filesResult.Value!.Count == 0)
            {
                Console.WriteLine($"   -> Übersprungen (Keine relevanten Dateien gefunden).");
                continue;
            }

            var feedResult = feedGenerator.GenerateFeed(solutionName, project, filesResult.Value);
            if (!feedResult.IsSuccess)
            {
                Console.WriteLine($"   -> [FEHLER] Generierung fehlgeschlagen: {feedResult.ErrorMessage}");
                continue;
            }

            // 5. Speichern
            var fileName = $"{solutionName}.{project.ProjectName}-{dateSuffix}.md";
            var filePath = Path.Combine(outputDir, fileName);

            File.WriteAllText(filePath, feedResult.Value!);
            Console.WriteLine($"   -> Gespeichert: {fileName} ({filesResult.Value.Count} Dateien)");
            successCount++;
        }

        Console.WriteLine("\n==================================================");
        Console.WriteLine($"- Fertig! {successCount} von {projects.Count} Projekten erfolgreich exportiert.");
        Console.WriteLine($"- Zu finden unter: {outputDir}");
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
