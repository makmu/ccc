using CCC.Domain;
using CCC.Domain.Events;
using CCC.Infrastructure.EventStore;

namespace CCC.Api.SubscriptionModels;

static class SubscriptionModelEndpoints
{
    public static void MapSubscriptionModelEndpoints(this WebApplication app)
    {
        app.MapPost("/subscription-models", async (AddSubscriptionModelRequest request, StateLoader loader) =>
        {
            var subscriptionIdTaken = State.SubscriptionExists(request.Id);

            var context = await loader.LoadAsync(subscriptionIdTaken);
                
            if (subscriptionIdTaken)
                return Results.Conflict("A subscription model with this id already exists.");

            try
            {
                await context.AppendAsync(new SubscriptionModelAdded(request.Id, request.Name, request.KeyLimit)
                ,Tags.From(Tag.SubscriptionId(request.Id)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
