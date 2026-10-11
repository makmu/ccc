using CCC.Base.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TranslationDelivery.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CCC.Api.TranslationDelivery.Queries;

static class LanguagesOfProjectEndpoint
{
    public static void MapLanguagesOfProject(this IEndpointRouteBuilder app) =>
        app.MapGet("/teams/{teamId}/projects/{projectId}/languages",
            async Task<Results<Ok<IReadOnlyList<string>>, NotFound<ProblemDetails>>> (Guid teamId, Guid projectId,
                IQueryHandler<LanguagesOfProject, IReadOnlyList<string>?> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new LanguagesOfProject(TeamId.From(teamId), ProjectId.From(projectId));
                var languages = await handler.HandleAsync(query, cancellationToken);
                return languages is null
                    ? TypedResults.NotFound(new ProblemDetails { Detail = "Project not found in this team." })
                    : TypedResults.Ok(languages);
            })
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
}
