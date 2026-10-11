using CCC.Api.AppAdministration.Commands;
using CCC.Api.AppAdministration.Queries;

namespace CCC.Api.AppAdministration;

static class EndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapAppAdministration(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags("AppAdministration");
        group.MapAddSubscriptionModel();
        group.MapRegisterTenant();
        group.MapAssignTenantOwner();
        group.MapAddUser();
        group.MapSubscriptionModels();
        group.MapTenants();
        group.MapUsers();
        return group;
    }
}
