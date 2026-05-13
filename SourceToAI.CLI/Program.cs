using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.App;
using SourceToAI.CLI.App.Cli;
using SourceToAI.CLI.App.Exceptions;
using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Infrastructure;
using SourceToAI.CLI.Services.Decompilation;
using SourceToAI.CLI.Services.Discovery;
using SourceToAI.CLI.Services.Export;
using SourceToAI.CLI.Services.Export.AiFeed;
using SourceToAI.CLI.Services.Processing;
using System.Collections.Generic;
using System.CommandLine;

var rootCommand = SourceToAiCli.CreateRootCommand(RunExportPipelineAsync);
var parseResult = rootCommand.Parse(args);

if (parseResult.Errors.Count > 0)
{
    foreach (var error in parseResult.Errors)
        await Console.Error.WriteLineAsync(error.Message);
    await Console.Error.WriteLineAsync(SourceToAiCli.Usage.UsageLine);
    Environment.ExitCode = 1;
    return;
}

Environment.ExitCode = await parseResult.InvokeAsync(parseResult.InvocationConfiguration);

static async Task<int> RunExportPipelineAsync(
    string exportPath,
    IReadOnlyList<string> solutionPaths,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    var appSettings = configuration.GetSection("SourceToAI").Get<AppSettings>()
                      ?? new AppSettings();

    var services = new ServiceCollection();
    services.AddSingleton(appSettings);
    services.AddTransient<IAssemblyDecompilerService, AssemblyDecompilerService>();
    services.AddTransient<ISolutionDiscoveryService, SolutionDiscoveryService>();
    services.AddSingleton<IDirectoryEnumerator, DefaultDirectoryEnumerator>();
    services.AddTransient<IFileDiscoveryService, FileDiscoveryService>();
    services.AddSingleton<ICSharpDocumentLoader, CSharpDocumentLoader>();
    services.AddTransient<IFeedGenerator, MarkdownFeedGenerator>();
    services.AddTransient<IDependencyGraphMarkdownGenerator, CsprojDependencyGraphMarkdownGenerator>();
    services.AddTransient<IMultiViewExportService, MultiViewExportService>();
    services.AddSingleton<IMultiViewReadmeMarkdownGenerator, MultiViewReadmeMarkdownGenerator>();
    services.AddSingleton<IAiFeedMarkdownComposer, AiFeedMarkdownComposer>();
    services.AddViewGenerators();
    services.AddMarkdownProjectViewBuilders();
    services.AddTransient<ConsoleOrchestrator>();

    var serviceProvider = services.BuildServiceProvider();

    try
    {
        var orchestrator = serviceProvider.GetRequiredService<ConsoleOrchestrator>();
        await orchestrator.RunAsync(solutionPaths, exportPath);
        return 0;
    }
    catch (SourceToAiValidationException ex)
    {
        Console.WriteLine($"[FEHLER] {ex.Message}");
        return 1;
    }
    catch (SourceToAiExportException ex)
    {
        Console.WriteLine($"[FEHLER] {ex.Message}");
        if (ex.InnerException is not null)
            Console.WriteLine(ex.InnerException.ToString());
        return 1;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FATAL ERROR] Ein unerwarteter Fehler ist aufgetreten: {ex.Message}");
        return 1;
    }
}
