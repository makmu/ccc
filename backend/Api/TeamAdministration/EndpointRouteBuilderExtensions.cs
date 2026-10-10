using CCC.Api.TeamAdministration.Commands;
using CCC.Api.TeamAdministration.Queries;

namespace CCC.Api.TeamAdministration;

static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTeamAdministration(this IEndpointRouteBuilder app)
    {
        app.MapAddProject();
        app.MapAddLanguage();
        app.MapProjectsOfTeam();
        return app;
    }
}
