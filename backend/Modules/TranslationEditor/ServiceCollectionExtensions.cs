using CCC.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.Modules.TranslationEditor;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTranslationEditor(this IServiceCollection services)
    {
        services.AddTransient<ICommandHandler<AddKey>, AddKeyHandler>();
        services.AddTransient<ICommandHandler<RenameKey>, RenameKeyHandler>();
        services.AddTransient<ICommandHandler<ProvideReferenceText>, ProvideReferenceTextHandler>();
        services.AddTransient<ICommandHandler<ProvideTranslation>, ProvideTranslationHandler>();
        return services;
    }
}
