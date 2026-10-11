using CCC.Base.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TeamAdministration.Queries;
using Microsoft.AspNetCore.Mvc;

namespace CCC.Api.TeamAdministration.Queries;

static class ProjectsOfTeamEndpoint
{
    public static void MapProjectsOfTeam(this IEndpointRouteBuilder app) =>
        app.MapGet("/teams/{teamId}/projects",
            async (Guid teamId, IQueryHandler<ProjectsOfTeam, IReadOnlyList<ProjectSummary>> handler,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(new ProjectsOfTeam(TeamId.From(teamId)), cancellationToken)))
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
}
