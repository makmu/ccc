using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TenantAdministration;

namespace CCC.Api.TenantAdministration;

static class DetailsOfTenantEndpoint
{
    public static void MapDetailsOfTenant(this IEndpointRouteBuilder app) =>
        app.MapGet("/tenants/{tenantId}",
            async (Guid tenantId, IQueryHandler<DetailsOfTenant, TenantDetails?> handler) =>
            {
                var details = await handler.HandleAsync(new DetailsOfTenant(TenantId.From(tenantId)));
                return details is null ? Results.NotFound("Tenant not found.") : Results.Ok(details);
            });
}
