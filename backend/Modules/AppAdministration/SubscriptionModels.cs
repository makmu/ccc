using CCC.Abstractions;
using CCC.EventStore;
using Dapper;

namespace CCC.Modules.AppAdministration;

public record SubscriptionModels;

public record SubscriptionModelSummary(Guid Id, string Name, int KeyLimit);

sealed class SubscriptionModelsHandler(ReadModelDatabase database)
    : IQueryHandler<SubscriptionModels, IReadOnlyList<SubscriptionModelSummary>>
{
    public async Task<IReadOnlyList<SubscriptionModelSummary>> HandleAsync(SubscriptionModels query)
    {
        await using var connection = await database.OpenAsync();
        var subscriptionModels = await connection.QueryAsync<SubscriptionModelSummary>("""
            SELECT subscription_model_id AS Id, name AS Name, key_limit AS KeyLimit
            FROM app_administration.subscription_models
            ORDER BY name
            """);
        return subscriptionModels.AsList();
    }
}
