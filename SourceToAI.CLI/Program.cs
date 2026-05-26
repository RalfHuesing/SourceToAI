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
    await Console.Error.WriteLineAsync(SourceToAiCli.Usage.UsageExamplePositional);
    await Console.Error.WriteLineAsync(SourceToAiCli.Usage.UsageExampleAssembly);
    await Console.Error.WriteLineAsync(SourceToAiCli.Usage.UsageExampleAssemblyWildcard);
    await Console.Error.WriteLineAsync(SourceToAiCli.Usage.UsageExampleGac);
    await Console.Error.WriteLineAsync(SourceToAiCli.Usage.UsageExampleOptions);
    await Console.Error.WriteLineAsync(SourceToAiCli.Usage.UsageExampleExclude);
    Environment.ExitCode = 1;
    return;
}

Environment.ExitCode = await parseResult.InvokeAsync(parseResult.InvocationConfiguration);

static async Task<int> RunExportPipelineAsync(
    string exportPath,
    IReadOnlyList<string> solutionPaths,
    IReadOnlyList<string> gacPatterns,
    IReadOnlyList<string> excludePatternsFromCli,
    int maxFileSize,
    int maxFileCount,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .Build();

    var appSettings = configuration.GetSection("SourceToAI").Get<AppSettings>()
                      ?? new AppSettings();

    if (maxFileSize > 0)
        appSettings.MaxFileSizeKb = maxFileSize;
    if (maxFileCount > 0)
        appSettings.MaxFileCount = maxFileCount;

    IReadOnlyList<GacAssemblyDiscovery.GacResolvedAssembly> fromGac;
    try
    {
        fromGac = gacPatterns.Count > 0
            ? GacAssemblyDiscovery.Resolve(
                gacPatterns,
                GacPathResolver.ResolveRoot(appSettings.GacAssemblyRoot))
            : [];
    }
    catch (SourceToAiValidationException ex)
    {
        await Console.Error.WriteLineAsync($"[FEHLER] {ex.Message}");
        return 1;
    }

    foreach (var resolved in fromGac)
    {
        Console.WriteLine(
            $"[INFO] GAC: {resolved.SimpleName} {resolved.Version} ({resolved.FlavorLabel}) -> {resolved.FullPath}");
    }

    var merged = solutionPaths.Concat(fromGac.Select(static r => r.FullPath)).ToList();

    IReadOnlyList<string> expandedPaths;
    try
    {
        expandedPaths = merged.Count > 0
            ? InputPathResolver.Resolve(merged)
            : [];
    }
    catch (SourceToAiValidationException ex)
    {
        await Console.Error.WriteLineAsync($"[FEHLER] {ex.Message}");
        return 1;
    }

    if (expandedPaths.Count == 0)
    {
        await Console.Error.WriteLineAsync($"[FEHLER] {SourceToAiCli.Usage.ErrorNoInputOrGac}");
        return 1;
    }

    foreach (var path in expandedPaths)
    {
        var validationError = ExportInputPathValidation.GetValidationError(path);
        if (validationError is not null)
        {
            await Console.Error.WriteLineAsync(validationError);
            return 1;
        }
    }

    if (excludePatternsFromCli.Count > 0)
    {
        var mergedExcludes = new List<string>();
        var fromConfig = appSettings.ExcludedPathPatterns;
        if (fromConfig is { Length: > 0 })
            mergedExcludes.AddRange(fromConfig);
        foreach (var p in excludePatternsFromCli)
            mergedExcludes.Add(p);
        appSettings.ExcludedPathPatterns = mergedExcludes.ToArray();
    }

    var services = new ServiceCollection();
    services.AddSingleton(appSettings);
    services.AddTransient<IAssemblyDecompilerService, AssemblyDecompilerService>();
    services.AddTransient<ISolutionDiscoveryService, SolutionDiscoveryService>();
    services.AddSingleton<IDirectoryEnumerator, DefaultDirectoryEnumerator>();
    services.AddTransient<IFileDiscoveryService, FileDiscoveryService>();
    services.AddSingleton<ICSharpDocumentLoader, CSharpDocumentLoader>();
    services.AddTransient<ProjectSplittingEngine>();
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
        var allAssemblySourcesOk = await orchestrator.RunAsync(expandedPaths, exportPath);
        return allAssemblySourcesOk ? 0 : 1;
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
