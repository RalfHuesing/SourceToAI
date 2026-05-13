using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.CLI.Services.Processing.Rewriters;
using SourceToAI.CLI.Services.Processing.ViewGenerators;

namespace SourceToAI.CLI.Infrastructure;

public static class ViewGeneratorServiceCollectionExtensions
{
    public static IServiceCollection AddViewGenerators(this IServiceCollection services)
    {
        services.AddKeyedTransient<IViewGenerator, CompleteViewGenerator>(MarkdownViewKeys.Complete);
        services.AddKeyedTransient<IViewGenerator>(
            MarkdownViewKeys.SignaturesOnly,
            (_, _) => new RoslynRewriteViewGenerator(MarkdownViewKeys.SignaturesOnly, SignaturesRewriter.Rewrite));
        services.AddKeyedTransient<IViewGenerator>(
            MarkdownViewKeys.PublicOnly,
            (_, _) => new RoslynRewriteViewGenerator(MarkdownViewKeys.PublicOnly, VisibilityRewriter.Rewrite));
        services.AddKeyedTransient<IViewGenerator>(
            MarkdownViewKeys.DtoOnly,
            (_, _) => new RoslynRewriteViewGenerator(MarkdownViewKeys.DtoOnly, DtoRewriter.Rewrite));
        return services;
    }
}
