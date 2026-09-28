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
                TenantId.From(request.Id),
                TenantName.From(request.Name),
                request.SubscriptionModelIds.Select(SubscriptionModelId.From).ToList());
            return (await handler.HandleAsync(command)).ToHttpResult();
        });

        app.MapPost("/tenants/{tenantId}/teams", async (Guid tenantId, AddTeamRequest request, ICommandHandler<AddTeam> handler) =>
        {
            var command = new AddTeam(
                TenantId.From(tenantId),
                TeamId.From(request.Id),
                TeamName.From(request.Name),
                SubscriptionModelId.From(request.SubscriptionModelId));
            return (await handler.HandleAsync(command)).ToHttpResult();
        });

        app.MapPost("/tenants/{tenantId}/owners", async (Guid tenantId, AssignTenantOwnerRequest request, ICommandHandler<AssignTenantOwner> handler) =>
        {
            var command = new AssignTenantOwner(TenantId.From(tenantId), UserId.From(request.UserId));
            return (await handler.HandleAsync(command)).ToHttpResult();
        });

        app.MapPost("/tenants/{tenantId}/rename", async (Guid tenantId, RenameTenantRequest request, ICommandHandler<RenameTenant> handler) =>
        {
            var command = new RenameTenant(TenantId.From(tenantId), TenantName.From(request.Name));
            return (await handler.HandleAsync(command)).ToHttpResult();
        });
    }
}
