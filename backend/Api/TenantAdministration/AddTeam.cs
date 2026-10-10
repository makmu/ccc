using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TenantAdministration;

namespace CCC.Api.TenantAdministration;

static class AddTeamEndpoint
{
    public record AddTeamRequest(Guid Id, string Name, Guid SubscriptionModelId);

    public static void MapAddTeam(this IEndpointRouteBuilder app) =>
        app.MapPost("/tenants/{tenantId}/teams",
            async (Guid tenantId, AddTeamRequest request, ICommandHandler<AddTeam> handler) =>
            {
                var command = new AddTeam(
                    TenantId.From(tenantId),
                    TeamId.From(request.Id),
                    TeamName.From(request.Name),
                    SubscriptionModelId.From(request.SubscriptionModelId));
                return (await handler.HandleAsync(command)).ToHttpResult();
            });
}
