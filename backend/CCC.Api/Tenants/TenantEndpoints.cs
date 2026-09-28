using CCC.Commands;
using CCC.Commands.Tenants;
using CCC.Domain.Types;

namespace CCC.Api.Tenants;

static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/tenants", async (RegisterTenantRequest request, ICommandHandler<RegisterTenant> handler) =>
        {
            var command = new RegisterTenant(
                TenantId.Create(request.Id),
                TenantName.Create(request.Name),
                request.SubscriptionModelIds.Select(SubscriptionModelId.Create).ToList());
            return (await handler.HandleAsync(command)).ToHttpResult();
        });

        app.MapPost("/tenants/{tenantId}/teams", async (Guid tenantId, AddTeamRequest request, ICommandHandler<AddTeam> handler) =>
        {
            var command = new AddTeam(
                TenantId.Create(tenantId),
                TeamId.Create(request.Id),
                TeamName.Create(request.Name),
                SubscriptionModelId.Create(request.SubscriptionModelId));
            return (await handler.HandleAsync(command)).ToHttpResult();
        });

        app.MapPost("/tenants/{tenantId}/owners", async (Guid tenantId, AssignTenantOwnerRequest request, ICommandHandler<AssignTenantOwner> handler) =>
        {
            var command = new AssignTenantOwner(TenantId.Create(tenantId), UserId.Create(request.UserId));
            return (await handler.HandleAsync(command)).ToHttpResult();
        });

        app.MapPost("/tenants/{tenantId}/rename", async (Guid tenantId, RenameTenantRequest request, ICommandHandler<RenameTenant> handler) =>
        {
            var command = new RenameTenant(TenantId.Create(tenantId), TenantName.Create(request.Name));
            return (await handler.HandleAsync(command)).ToHttpResult();
        });
    }
}
