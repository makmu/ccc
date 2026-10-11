using CCC.Base.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TenantAdministration.Queries;
using Microsoft.AspNetCore.Mvc;

namespace CCC.Api.TenantAdministration.Queries;

static class TeamsOfTenantEndpoint
{
    public static void MapTeamsOfTenant(this IEndpointRouteBuilder app) =>
        app.MapGet("/tenants/{tenantId}/teams",
            async (Guid tenantId, IQueryHandler<TeamsOfTenant, IReadOnlyList<TeamSummary>> handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(new TeamsOfTenant(TenantId.From(tenantId)), cancellationToken)))
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
}
