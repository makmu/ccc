using CCC.Api.TranslationDelivery.Queries;

namespace CCC.Api.TranslationDelivery;

static class EndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapTranslationDelivery(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags("TranslationDelivery");
        group.MapTranslationsOfProject();
        group.MapLanguagesOfProject();
        return group;
    }
}
