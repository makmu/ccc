using CCC.Commands;
using CCC.Commands.Tenants;
using CCC.Domain.Types;

namespace CCC.Api.Tenants;

static class RenameTenantEndpoint
{
    public record RenameTenantRequest(string Name);

    public static void MapRenameTenant(this IEndpointRouteBuilder app) =>
        app.MapPost("/tenants/{tenantId}/rename",
            async (Guid tenantId, RenameTenantRequest request, ICommandHandler<RenameTenant> handler) =>
            {
                var command = new RenameTenant(TenantId.From(tenantId), TenantName.From(request.Name));
                return (await handler.HandleAsync(command)).ToHttpResult();
            });
}
