using CCC.Abstractions;
using CCC.EventStore;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.ReadModels.PublicTranslations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPublicTranslations(this IServiceCollection services)
    {
        services.AddProjection<TranslationsProjection>();
        services.AddTransient<IQueryHandler<TranslationsOfProject, IReadOnlyList<Translation>?>, TranslationsOfProjectHandler>();
        return services;
    }
}
