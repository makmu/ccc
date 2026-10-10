using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.AppAdministration;

namespace CCC.Api.AppAdministration;

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
