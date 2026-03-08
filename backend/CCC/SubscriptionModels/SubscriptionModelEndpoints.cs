using CCC.Infrastructure.EventStore;

namespace CCC.SubscriptionModels;

static class SubscriptionModelEndpoints
{
    public static void MapSubscriptionModelEndpoints(this WebApplication app)
    {
        app.MapPost("/subscription-models", async (AddSubscriptionModelRequest request, CommandContextBuilder contextBuilder) =>
        {
            var context = await contextBuilder
                .Where<SubscriptionModelAdded>(w => w.With(e => e.Id, request.Id))
                .LoadAsync();

            if (context.Events.Count != 0)
                return Results.Conflict("A subscription model with this id already exists.");

            try
            {
                await context.AppendAsync(new SubscriptionModelAdded(request.Id, request.Name, request.KeyLimit));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
