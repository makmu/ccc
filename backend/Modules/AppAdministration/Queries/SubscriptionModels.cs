using CCC.Base.Abstractions;
using CCC.Base.Projections;
using Dapper;

namespace CCC.Modules.AppAdministration.Queries;

public record SubscriptionModels;

public record SubscriptionModelSummary(Guid Id, string Name, int KeyLimit);

sealed class SubscriptionModelsHandler(ProjectionDatabase database)
    : IQueryHandler<SubscriptionModels, IReadOnlyList<SubscriptionModelSummary>>
{
    public async Task<IReadOnlyList<SubscriptionModelSummary>> HandleAsync(SubscriptionModels query, CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        var subscriptionModels = await connection.QueryAsync<SubscriptionModelSummary>(new CommandDefinition("""
            SELECT subscription_model_id AS Id, name AS Name, key_limit AS KeyLimit
            FROM app_administration.subscription_models
            ORDER BY name
            """, cancellationToken: cancellationToken));
        return subscriptionModels.AsList();
    }
}
