using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.Infrastructure;
using SourceToAI.CLI.Services.Export;
using SourceToAI.CLI.Services.Export.AiFeed;
using SourceToAI.CLI.Services.Processing;

namespace SourceToAI.Tests.App;

/// <summary>
/// DI-Container wie im Multi-View-Integrationstest, damit E2E-Tests dieselbe Pipeline nutzen.
/// </summary>
public static class MultiViewExportTestHost
{
    public static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICSharpDocumentLoader, CSharpDocumentLoader>();
        services.AddViewGenerators();
        services.AddMarkdownProjectViewBuilders();
        services.AddSingleton<IAiFeedMarkdownComposer, AiFeedMarkdownComposer>();
        services.AddTransient<IMultiViewExportService, MultiViewExportService>();
        services.AddSingleton<IMultiViewReadmeMarkdownGenerator, MultiViewReadmeMarkdownGenerator>();
        return services.BuildServiceProvider();
    }
}
