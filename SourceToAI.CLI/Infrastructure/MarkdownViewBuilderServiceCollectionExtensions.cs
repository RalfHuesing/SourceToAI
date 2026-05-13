using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.CLI.Services.Processing.Markdown;

namespace SourceToAI.CLI.Infrastructure;

public static class MarkdownViewBuilderServiceCollectionExtensions
{
    public static IServiceCollection AddMarkdownProjectViewBuilders(this IServiceCollection services)
    {
        services.AddTransient<IMarkdownProjectViewBuilder>(sp => new MarkdownProjectViewBuilderBase(
            sp.GetRequiredService<ICSharpDocumentLoader>(),
            sp.GetRequiredKeyedService<IViewGenerator>(MarkdownViewKeys.Complete),
            includeNonCSharpFiles: true,
            passOriginalSourceTextForCSharp: true));

        services.AddTransient<IMarkdownProjectViewBuilder>(sp => new MarkdownProjectViewBuilderBase(
            sp.GetRequiredService<ICSharpDocumentLoader>(),
            sp.GetRequiredKeyedService<IViewGenerator>(MarkdownViewKeys.SignaturesOnly),
            includeNonCSharpFiles: false,
            passOriginalSourceTextForCSharp: false));

        services.AddTransient<IMarkdownProjectViewBuilder>(sp => new MarkdownProjectViewBuilderBase(
            sp.GetRequiredService<ICSharpDocumentLoader>(),
            sp.GetRequiredKeyedService<IViewGenerator>(MarkdownViewKeys.PublicOnly),
            includeNonCSharpFiles: false,
            passOriginalSourceTextForCSharp: false));

        services.AddTransient<IMarkdownProjectViewBuilder>(sp => new MarkdownProjectViewBuilderBase(
            sp.GetRequiredService<ICSharpDocumentLoader>(),
            sp.GetRequiredKeyedService<IViewGenerator>(MarkdownViewKeys.DtoOnly),
            includeNonCSharpFiles: false,
            passOriginalSourceTextForCSharp: false));

        return services;
    }
}
