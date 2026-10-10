using CCC.Abstractions;
using CCC.Modules.AppAdministration;

namespace CCC.Api.AppAdministration;

static class SubscriptionModelsEndpoint
{
    public static void MapSubscriptionModels(this IEndpointRouteBuilder app) =>
        app.MapGet("/subscription-models",
            async (IQueryHandler<SubscriptionModels, IReadOnlyList<SubscriptionModelSummary>> handler) =>
                Results.Ok(await handler.HandleAsync(new SubscriptionModels())));
}
