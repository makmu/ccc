using CCC.Base.Abstractions;
using CCC.Modules.AppAdministration.Queries;

namespace CCC.Api.AppAdministration.Queries;

static class TenantsEndpoint
{
    public static void MapTenants(this IEndpointRouteBuilder app) =>
        app.MapGet("/tenants",
            async (IQueryHandler<Tenants, IReadOnlyList<TenantSummary>> handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(new Tenants(), cancellationToken)));
}
