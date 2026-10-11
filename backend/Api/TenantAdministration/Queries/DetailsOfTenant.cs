using CCC.Base.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TenantAdministration.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CCC.Api.TenantAdministration.Queries;

static class DetailsOfTenantEndpoint
{
    public static void MapDetailsOfTenant(this IEndpointRouteBuilder app) =>
        app.MapGet("/tenants/{tenantId}",
            async Task<Results<Ok<TenantDetails>, NotFound<ProblemDetails>>> (Guid tenantId,
                IQueryHandler<DetailsOfTenant, TenantDetails?> handler, CancellationToken cancellationToken) =>
            {
                var details = await handler.HandleAsync(new DetailsOfTenant(TenantId.From(tenantId)), cancellationToken);
                return details is null ? TypedResults.NotFound(new ProblemDetails { Detail = "Tenant not found." }) : TypedResults.Ok(details);
            })
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
}
