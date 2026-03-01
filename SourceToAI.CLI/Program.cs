using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.App;
using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Services.Discovery;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.CLI.Services.Integration;

// 1. CLI Argumente prüfen (Jetzt 2 Argumente erforderlich)
if (args.Length < 2 || string.IsNullOrWhiteSpace(args[0]) || string.IsNullOrWhiteSpace(args[1]))
{
    Console.WriteLine("Verwendung: SourceToAI.exe <Export-Pfad> <Pfad-zur-Solution>");
    Console.WriteLine("Beispiel: SourceToAI.exe ./exports C:\\Daten\\MeineSolution\\");
    return;
}

string exportPath = args[0];   // Erstes Argument: Wo soll es hin?
string solutionPath = args[1]; // Zweites Argument: Was soll gelesen werden?

// 2. Konfiguration laden
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var appSettings = configuration.GetSection("SourceToAI").Get<AppSettings>()
                  ?? new AppSettings();

var googleDriveSettings = configuration.GetSection("GoogleDriveSync").Get<GoogleDriveSyncSettings>()
                          ?? new GoogleDriveSyncSettings();

// 3. Dependency Injection Container aufbauen
var services = new ServiceCollection();

// Config registrieren
services.AddSingleton(appSettings);
services.AddSingleton(googleDriveSettings);

// Discovery Services registrieren
services.AddTransient<ISolutionDiscoveryService, SolutionDiscoveryService>();
services.AddTransient<IFileDiscoveryService, FileDiscoveryService>();

// Processing Services registrieren
services.AddTransient<IFileTypeService, FileTypeService>();
services.AddTransient<IHashService, HashService>();
services.AddTransient<IFeedGenerator, MarkdownFeedGenerator>();

// Integration Services registrieren
services.AddTransient<IGoogleDriveClient, GoogleDriveClient>();
services.AddTransient<IPostExportTask, GoogleDriveSyncService>();

// App registrieren
services.AddTransient<ConsoleOrchestrator>();

var serviceProvider = services.BuildServiceProvider();

// 4. Anwendung starten
try
{
    var orchestrator = serviceProvider.GetRequiredService<ConsoleOrchestrator>();
    await orchestrator.RunAsync(solutionPath, exportPath);
}
catch (Exception ex)
{
    Console.WriteLine($"[FATAL ERROR] Ein unerwarteter Fehler ist aufgetreten: {ex.Message}");
}
