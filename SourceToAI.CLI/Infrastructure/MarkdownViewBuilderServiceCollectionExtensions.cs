using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.Services.Processing.Markdown;

namespace SourceToAI.CLI.Infrastructure;

public static class MarkdownViewBuilderServiceCollectionExtensions
{
    public static IServiceCollection AddMarkdownProjectViewBuilders(this IServiceCollection services)
    {
        services.AddTransient<IMarkdownProjectViewBuilder, CompleteMarkdownProjectViewBuilder>();
        services.AddTransient<IMarkdownProjectViewBuilder, SignaturesOnlyMarkdownProjectViewBuilder>();
        services.AddTransient<IMarkdownProjectViewBuilder, PublicOnlyMarkdownProjectViewBuilder>();
        services.AddTransient<IMarkdownProjectViewBuilder, DtoOnlyMarkdownProjectViewBuilder>();
        return services;
    }
}
