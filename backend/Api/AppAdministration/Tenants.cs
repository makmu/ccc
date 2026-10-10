using CCC.Abstractions;
using CCC.Modules.AppAdministration;

namespace CCC.Api.AppAdministration;

static class TenantsEndpoint
{
    public static void MapTenants(this IEndpointRouteBuilder app) =>
        app.MapGet("/tenants",
            async (IQueryHandler<Tenants, IReadOnlyList<TenantSummary>> handler) =>
                Results.Ok(await handler.HandleAsync(new Tenants())));
}
