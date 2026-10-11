using CCC.Base.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.AppAdministration.Commands;

namespace CCC.Api.AppAdministration.Commands;

static class RegisterTenantEndpoint
{
    public sealed record RegisterTenantRequest(Guid Id, string Name, IReadOnlyList<Guid> SubscriptionModelIds);

    public static void MapRegisterTenant(this IEndpointRouteBuilder app) =>
        app.MapPost("/tenants",
            async (RegisterTenantRequest request, ICommandHandler<RegisterTenant> handler, CancellationToken cancellationToken) =>
            {
                var command = new RegisterTenant(
                    TenantId.From(request.Id),
                    TenantName.From(request.Name),
                    request.SubscriptionModelIds.Select(SubscriptionModelId.From).ToList());
                return (await handler.HandleAsync(command, cancellationToken)).ToHttpResult();
            });
}
