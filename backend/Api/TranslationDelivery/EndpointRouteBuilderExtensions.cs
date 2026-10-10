using CCC.Api.TranslationDelivery.Queries;

namespace CCC.Api.TranslationDelivery;

static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTranslationDelivery(this IEndpointRouteBuilder app)
    {
        app.MapTranslationsOfProject();
        app.MapLanguagesOfProject();
        return app;
    }
}
