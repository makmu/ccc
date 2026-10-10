using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.EventStore;
using Dapper;

namespace CCC.Modules.TenantAdministration.Queries;

public record TeamsOfTenant(TenantId TenantId);

public record TeamSummary(Guid Id, string Name, string SubscriptionModel);

sealed class TeamsOfTenantHandler(ReadModelDatabase database) : IQueryHandler<TeamsOfTenant, IReadOnlyList<TeamSummary>>
{
    public async Task<IReadOnlyList<TeamSummary>> HandleAsync(TeamsOfTenant query)
    {
        await using var connection = await database.OpenAsync();
        var teams = await connection.QueryAsync<TeamSummary>("""
            SELECT t.team_id AS Id, t.name AS Name, s.name AS SubscriptionModel
            FROM tenant_administration.teams t
            JOIN tenant_administration.subscription_models s ON s.subscription_model_id = t.subscription_model_id
            WHERE t.tenant_id = @TenantId
            ORDER BY t.name
            """, new { TenantId = query.TenantId.Value });
        return teams.AsList();
    }
}
