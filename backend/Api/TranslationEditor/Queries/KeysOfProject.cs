using CCC.Base.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TranslationEditor.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CCC.Api.TranslationEditor.Queries;

static class KeysOfProjectEndpoint
{
    public static void MapKeysOfProject(this IEndpointRouteBuilder app) =>
        app.MapGet("/teams/{teamId}/projects/{projectId}/keys",
            async Task<Results<Ok<ProjectKeys>, NotFound<ProblemDetails>>> (Guid teamId, Guid projectId,
                IQueryHandler<KeysOfProject, ProjectKeys?> handler, CancellationToken cancellationToken) =>
            {
                var keys = await handler.HandleAsync(new KeysOfProject(TeamId.From(teamId), ProjectId.From(projectId)), cancellationToken);
                return keys is null
                    ? TypedResults.NotFound(new ProblemDetails { Detail = "Project not found in this team." })
                    : TypedResults.Ok(keys);
            })
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
}
