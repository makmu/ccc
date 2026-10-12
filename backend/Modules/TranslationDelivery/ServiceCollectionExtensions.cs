using CCC.Base.Abstractions;
using CCC.Base.ModuleHosting;
using CCC.Base.Projections;
using CCC.Modules.TranslationDelivery.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.Modules.TranslationDelivery;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTranslationDelivery(this IServiceCollection services)
    {
        services.AddTransient<IQueryHandler<TranslationsOfProject, IReadOnlyList<Translation>?>, TranslationsOfProjectHandler>();
        services.AddTransient<IQueryHandler<LanguagesOfProject, IReadOnlyList<string>?>, LanguagesOfProjectHandler>();
        services.AddModule<TranslationDeliveryModule>()
            .AddProjection<TranslationsProjection>();
        return services;
    }
}
