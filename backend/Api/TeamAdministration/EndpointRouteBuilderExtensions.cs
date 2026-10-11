using CCC.Api.TeamAdministration.Commands;
using CCC.Api.TeamAdministration.Queries;

namespace CCC.Api.TeamAdministration;

static class EndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapTeamAdministration(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags("TeamAdministration");
        group.MapAddProject();
        group.MapAddLanguage();
        group.MapProjectsOfTeam();
        return group;
    }
}
