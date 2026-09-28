using CCC.Commands;
using CCC.Commands.Tenants;
using CCC.Domain.Types;

namespace CCC.Api.Tenants;

static class RegisterTenantEndpoint
{
    public record RegisterTenantRequest(Guid Id, string Name, IReadOnlyList<Guid> SubscriptionModelIds);

    public static void MapRegisterTenant(this IEndpointRouteBuilder app) =>
        app.MapPost("/tenants",
            async (RegisterTenantRequest request, ICommandHandler<RegisterTenant> handler) =>
            {
                var command = new RegisterTenant(
                    TenantId.From(request.Id),
                    TenantName.From(request.Name),
                    request.SubscriptionModelIds.Select(SubscriptionModelId.From).ToList());
                return (await handler.HandleAsync(command)).ToHttpResult();
            });
}
