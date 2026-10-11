using CCC.Base.Abstractions;
using CCC.Modules.AppAdministration.Queries;

namespace CCC.Api.AppAdministration.Queries;

static class SubscriptionModelsEndpoint
{
    public static void MapSubscriptionModels(this IEndpointRouteBuilder app) =>
        app.MapGet("/subscription-models",
            async (IQueryHandler<SubscriptionModels, IReadOnlyList<SubscriptionModelSummary>> handler,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(new SubscriptionModels(), cancellationToken)));
}
