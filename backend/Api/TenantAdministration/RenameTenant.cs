using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TenantAdministration;

namespace CCC.Api.TenantAdministration;

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
