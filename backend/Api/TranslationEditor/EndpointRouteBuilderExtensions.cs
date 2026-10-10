using CCC.Api.TranslationEditor.Commands;
using CCC.Api.TranslationEditor.Queries;

namespace CCC.Api.TranslationEditor;

static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTranslationEditor(this IEndpointRouteBuilder app)
    {
        app.MapAddKey();
        app.MapRenameKey();
        app.MapProvideReferenceText();
        app.MapProvideTranslation();
        app.MapKeysOfProject();
        return app;
    }
}
