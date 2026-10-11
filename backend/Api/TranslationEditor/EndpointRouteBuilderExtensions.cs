using CCC.Api.TranslationEditor.Commands;
using CCC.Api.TranslationEditor.Queries;

namespace CCC.Api.TranslationEditor;

static class EndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapTranslationEditor(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags("TranslationEditor");
        group.MapAddKey();
        group.MapRenameKey();
        group.MapProvideReferenceText();
        group.MapProvideTranslation();
        group.MapKeysOfProject();
        return group;
    }
}
