using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.AppAdministration.Commands;

namespace CCC.Api.AppAdministration.Commands;

static class AddSubscriptionModelEndpoint
{
    public record AddSubscriptionModelRequest(Guid Id, string Name, int KeyLimit);

    public static void MapAddSubscriptionModel(this IEndpointRouteBuilder app) =>
        app.MapPost("/subscription-models",
            async (AddSubscriptionModelRequest request, ICommandHandler<AddSubscriptionModel> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new AddSubscriptionModel(
                    SubscriptionModelId.From(request.Id),
                    SubscriptionModelName.From(request.Name),
                    KeyLimit.From(request.KeyLimit));
                return (await handler.HandleAsync(command, cancellationToken)).ToHttpResult();
            });
}
