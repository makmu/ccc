using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.AppAdministration;

namespace CCC.Api.AppAdministration;

static class AssignTenantOwnerEndpoint
{
    public record AssignTenantOwnerRequest(Guid UserId);

    public static void MapAssignTenantOwner(this IEndpointRouteBuilder app) =>
        app.MapPost("/tenants/{tenantId}/owners",
            async (Guid tenantId, AssignTenantOwnerRequest request, ICommandHandler<AssignTenantOwner> handler) =>
            {
                var command = new AssignTenantOwner(TenantId.From(tenantId), UserId.From(request.UserId));
                return (await handler.HandleAsync(command)).ToHttpResult();
            });
}
