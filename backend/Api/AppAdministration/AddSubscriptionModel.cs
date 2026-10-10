using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.AppAdministration;

namespace CCC.Api.AppAdministration;

static class AddSubscriptionModelEndpoint
{
    public record AddSubscriptionModelRequest(Guid Id, string Name, int KeyLimit);

    public static void MapAddSubscriptionModel(this IEndpointRouteBuilder app) =>
        app.MapPost("/subscription-models",
            async (AddSubscriptionModelRequest request, ICommandHandler<AddSubscriptionModel> handler) =>
            {
                var command = new AddSubscriptionModel(
                    SubscriptionModelId.From(request.Id),
                    request.Name,
                    KeyLimit.From(request.KeyLimit));
                return (await handler.HandleAsync(command)).ToHttpResult();
            });
}
