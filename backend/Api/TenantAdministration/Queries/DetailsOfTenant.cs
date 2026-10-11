using CCC.Base.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TenantAdministration.Queries;

namespace CCC.Api.TenantAdministration.Queries;

static class DetailsOfTenantEndpoint
{
    public static void MapDetailsOfTenant(this IEndpointRouteBuilder app) =>
        app.MapGet("/tenants/{tenantId}",
            async (Guid tenantId, IQueryHandler<DetailsOfTenant, TenantDetails?> handler, CancellationToken cancellationToken) =>
            {
                var details = await handler.HandleAsync(new DetailsOfTenant(TenantId.From(tenantId)), cancellationToken);
                return details is null ? Results.NotFound("Tenant not found.") : Results.Ok(details);
            });
}
