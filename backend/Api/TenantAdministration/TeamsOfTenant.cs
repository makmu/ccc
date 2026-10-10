using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TenantAdministration;

namespace CCC.Api.TenantAdministration;

static class TeamsOfTenantEndpoint
{
    public static void MapTeamsOfTenant(this IEndpointRouteBuilder app) =>
        app.MapGet("/tenants/{tenantId}/teams",
            async (Guid tenantId, IQueryHandler<TeamsOfTenant, IReadOnlyList<TeamSummary>> handler) =>
                Results.Ok(await handler.HandleAsync(new TeamsOfTenant(TenantId.From(tenantId)))));
}
