using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.ValueObjects;
using CCC.EventStore;

namespace CCC.Api.SubscriptionModels;

static class SubscriptionModelEndpoints
{
    public static void MapSubscriptionModelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/subscription-models", async (AddSubscriptionModelRequest request, IStateLoader loader) =>
        {
            var subscriptionId = SubscriptionModelId.Create(request.Id);
            var keyLimit = KeyLimit.Create(request.KeyLimit);

            var subscriptionIdTaken = State.SubscriptionExists(subscriptionId);

            var context = await loader.LoadAsync(subscriptionIdTaken);

            if (subscriptionIdTaken)
                return Results.Conflict("A subscription model with this id already exists.");

            try
            {
                await context.AppendAsync(new SubscriptionModelAdded(request.Id, request.Name, keyLimit.Value),
                    Tags.From(Tag.SubscriptionId(subscriptionId)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
