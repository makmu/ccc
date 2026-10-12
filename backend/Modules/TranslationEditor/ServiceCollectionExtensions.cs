using CCC.Base.Abstractions;
using CCC.Base.ModuleHosting;
using CCC.Base.Projections;
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
        services.AddTransient<IQueryHandler<KeysOfProject, ProjectKeys?>, KeysOfProjectHandler>();
        services.AddModule<TranslationEditorModule>()
            .AddProjection<TranslationEditorProjection>();
        return services;
    }
}
