using CCC.Abstractions;
using CCC.EventStore;
using CCC.Modules.TranslationEditor.Commands;
using CCC.Modules.TranslationEditor.Queries;
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
        services.AddProjection<TranslationEditorProjection>();
        services.AddTransient<IQueryHandler<KeysOfProject, ProjectKeys?>, KeysOfProjectHandler>();
        return services;
    }
}
