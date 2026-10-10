using CCC.Api.AppAdministration.Commands;
using CCC.Api.AppAdministration.Queries;

namespace CCC.Api.AppAdministration;

static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAppAdministration(this IEndpointRouteBuilder app)
    {
        app.MapAddSubscriptionModel();
        app.MapRegisterTenant();
        app.MapAssignTenantOwner();
        app.MapAddUser();
        app.MapSubscriptionModels();
        app.MapTenants();
        app.MapUsers();
        return app;
    }
}
