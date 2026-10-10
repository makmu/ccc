using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TeamAdministration;

namespace CCC.Api.TeamAdministration;

static class ProjectsOfTeamEndpoint
{
    public static void MapProjectsOfTeam(this IEndpointRouteBuilder app) =>
        app.MapGet("/teams/{teamId}/projects",
            async (Guid teamId, IQueryHandler<ProjectsOfTeam, IReadOnlyList<ProjectSummary>> handler) =>
                Results.Ok(await handler.HandleAsync(new ProjectsOfTeam(TeamId.From(teamId)))));
}
