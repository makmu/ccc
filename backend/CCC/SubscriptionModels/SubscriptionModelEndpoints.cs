using CCC.Infrastructure.EventStore;

namespace CCC.SubscriptionModels;

static class SubscriptionModelEndpoints
{
    public static void MapSubscriptionModelEndpoints(this WebApplication app)
    {
        app.MapPost("/subscription-models", async (AddSubscriptionModelRequest request, CommandContextBuilder contextBuilder) =>
        {
            var subscriptionIdTaken = StateModel.Init(false).
                Where("subscription-id", request.Id.ToString())
                .When<SubscriptionModelAdded>((_, _) => true);

            var context = await contextBuilder.BuildStateModelsAsync(subscriptionIdTaken);
                
            if (subscriptionIdTaken.State)
                return Results.Conflict("A subscription model with this id already exists.");

            try
            {
                await context.AppendAsync(new SubscriptionModelAdded(request.Id, request.Name, request.KeyLimit)
                ,Tags.From(("subscription-id", request.Id.ToString())));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
