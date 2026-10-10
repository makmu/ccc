using CCC.Abstractions;
using CCC.EventStore;
using CCC.Modules.TranslationDelivery.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.Modules.TranslationDelivery;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTranslationDelivery(this IServiceCollection services)
    {
        services.AddProjection<TranslationsProjection>();
        services.AddTransient<IQueryHandler<TranslationsOfProject, IReadOnlyList<Translation>?>, TranslationsOfProjectHandler>();
        services.AddTransient<IQueryHandler<LanguagesOfProject, IReadOnlyList<string>?>, LanguagesOfProjectHandler>();
        return services;
    }
}
