using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.App;
using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Services.Discovery;
using SourceToAI.CLI.Services.Processing;

// 1. CLI Argumente prüfen
if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.WriteLine("Verwendung: SourceToAI.exe <Pfad-zur-Solution>");
    Console.WriteLine("Beispiel: SourceToAI.exe C:\\Daten\\MeineSolution\\");
    return;
}

string targetPath = args[0];

// 2. Konfiguration laden
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var appSettings = new AppSettings();
configuration.GetSection("SourceToAI").Bind(appSettings);

// 3. Dependency Injection Container aufbauen
var services = new ServiceCollection();

// Config registrieren
services.AddSingleton(appSettings);

// Discovery Services registrieren
services.AddTransient<ISolutionDiscoveryService, SolutionDiscoveryService>();
services.AddTransient<IFileDiscoveryService, FileDiscoveryService>();

// Processing Services registrieren
services.AddTransient<IFileTypeService, FileTypeService>();
services.AddTransient<IHashService, HashService>();
services.AddTransient<IFeedGenerator, MarkdownFeedGenerator>();

// App registrieren
services.AddTransient<ConsoleOrchestrator>();

var serviceProvider = services.BuildServiceProvider();

// 4. Anwendung starten
try
{
    var orchestrator = serviceProvider.GetRequiredService<ConsoleOrchestrator>();
    orchestrator.Run(targetPath);
}
catch (Exception ex)
{
    Console.WriteLine($"[FATAL ERROR] Ein unerwarteter Fehler ist aufgetreten: {ex.Message}");
}