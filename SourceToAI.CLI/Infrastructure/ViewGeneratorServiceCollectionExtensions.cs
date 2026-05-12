using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.CLI.Services.Processing.ViewGenerators;

namespace SourceToAI.CLI.Infrastructure;

public static class ViewGeneratorServiceCollectionExtensions
{
    public static IServiceCollection AddViewGenerators(this IServiceCollection services)
    {
        services.AddTransient<IViewGenerator, CompleteViewGenerator>();
        services.AddTransient<IViewGenerator, SignaturesOnlyViewGenerator>();
        services.AddTransient<IViewGenerator, PublicOnlyViewGenerator>();
        services.AddTransient<IViewGenerator, DtoOnlyViewGenerator>();
        return services;
    }
}
