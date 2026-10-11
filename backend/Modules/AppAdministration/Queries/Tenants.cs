using CCC.Abstractions;
using CCC.ReadModels;
using Dapper;

namespace CCC.Modules.AppAdministration.Queries;

public record Tenants;

public record TenantSummary(Guid Id, string Name, string[] SubscriptionModels, string[] Owners);

sealed class TenantsHandler(ReadModelDatabase database) : IQueryHandler<Tenants, IReadOnlyList<TenantSummary>>
{
    public async Task<IReadOnlyList<TenantSummary>> HandleAsync(Tenants query, CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        var tenants = await connection.QueryAsync<TenantSummary>(new CommandDefinition("""
            SELECT
                t.tenant_id AS Id,
                t.name AS Name,
                ARRAY(
                    SELECT s.name
                    FROM app_administration.tenant_subscription_models ts
                    JOIN app_administration.subscription_models s ON s.subscription_model_id = ts.subscription_model_id
                    WHERE ts.tenant_id = t.tenant_id
                    ORDER BY s.name
                ) AS SubscriptionModels,
                ARRAY(
                    SELECT u.name
                    FROM app_administration.tenant_owners o
                    JOIN app_administration.users u ON u.user_id = o.user_id
                    WHERE o.tenant_id = t.tenant_id
                    ORDER BY u.name
                ) AS Owners
            FROM app_administration.tenants t
            ORDER BY t.name
            """, cancellationToken: cancellationToken));
        return tenants.AsList();
    }
}
