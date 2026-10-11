using CCC.Base.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TranslationDelivery.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CCC.Api.TranslationDelivery.Queries;

static class TranslationsOfProjectEndpoint
{
    public static void MapTranslationsOfProject(this IEndpointRouteBuilder app) =>
        app.MapGet("/teams/{teamId}/projects/{projectId}/languages/{languageCode}/translations",
            async Task<Results<Ok<IReadOnlyList<Translation>>, NotFound<ProblemDetails>>> (Guid teamId, Guid projectId,
                string languageCode, IQueryHandler<TranslationsOfProject, IReadOnlyList<Translation>?> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new TranslationsOfProject(
                    TeamId.From(teamId),
                    ProjectId.From(projectId),
                    LanguageCode.From(languageCode));
                var translations = await handler.HandleAsync(query, cancellationToken);
                return translations is null
                    ? TypedResults.NotFound(new ProblemDetails { Detail = "Language not found in this project." })
                    : TypedResults.Ok(translations);
            })
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
}
