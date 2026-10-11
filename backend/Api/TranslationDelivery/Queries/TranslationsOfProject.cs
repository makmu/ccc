using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TranslationDelivery.Queries;

namespace CCC.Api.TranslationDelivery.Queries;

static class TranslationsOfProjectEndpoint
{
    public static void MapTranslationsOfProject(this IEndpointRouteBuilder app) =>
        app.MapGet("/teams/{teamId}/projects/{projectId}/languages/{languageCode}/translations",
            async (Guid teamId, Guid projectId, string languageCode, IQueryHandler<TranslationsOfProject, IReadOnlyList<Translation>?> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new TranslationsOfProject(
                    TeamId.From(teamId),
                    ProjectId.From(projectId),
                    LanguageCode.From(languageCode));
                var translations = await handler.HandleAsync(query, cancellationToken);
                return translations is null
                    ? Results.NotFound("Language not found in this project.")
                    : Results.Ok(translations);
            });
}
