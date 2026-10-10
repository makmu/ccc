using CCC.Api.TenantAdministration.Commands;
using CCC.Api.TenantAdministration.Queries;

namespace CCC.Api.TenantAdministration;

static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTenantAdministration(this IEndpointRouteBuilder app)
    {
        app.MapAddTeam();
        app.MapRenameTenant();
        app.MapDetailsOfTenant();
        app.MapTeamsOfTenant();
        return app;
    }
}
