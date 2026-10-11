using CCC.Api.TenantAdministration.Commands;
using CCC.Api.TenantAdministration.Queries;

namespace CCC.Api.TenantAdministration;

static class EndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapTenantAdministration(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags("TenantAdministration");
        group.MapAddTeam();
        group.MapRenameTenant();
        group.MapDetailsOfTenant();
        group.MapTeamsOfTenant();
        return group;
    }
}
