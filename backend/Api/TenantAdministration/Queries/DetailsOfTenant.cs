using CCC.Base.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TenantAdministration.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CCC.Api.TenantAdministration.Queries;

static class DetailsOfTenantEndpoint
{
    public static void MapDetailsOfTenant(this IEndpointRouteBuilder app) =>
        app.MapGet("/tenants/{tenantId}",
            async Task<Results<Ok<TenantDetails>, ProblemHttpResult>> (Guid tenantId,
                IQueryHandler<DetailsOfTenant, TenantDetails?> handler, CancellationToken cancellationToken) =>
            {
                var details = await handler.HandleAsync(new DetailsOfTenant(TenantId.From(tenantId)), cancellationToken);
                return details is null
                    ? TypedResults.Problem("Tenant not found.", statusCode: StatusCodes.Status404NotFound)
                    : TypedResults.Ok(details);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
}
