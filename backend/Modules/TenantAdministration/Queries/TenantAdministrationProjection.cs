using System.Text.Json;
using CCC.Domain.Events;
using CCC.EventStore;
using Dapper;
using Npgsql;

namespace CCC.Modules.TenantAdministration.Queries;

sealed class TenantAdministrationProjection : IProjection
{
    public string Schema => "tenant_administration";

    public int Version => 1;

    public IReadOnlyCollection<string> EventTypes { get; } =
    [
        nameof(SubscriptionModelAdded),
        nameof(TeamAdded),
    ];

    public Task InstallAsync(NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS tenant_administration.subscription_models (
                subscription_model_id UUID PRIMARY KEY,
                name                  TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS tenant_administration.teams (
                team_id               UUID PRIMARY KEY,
                tenant_id             UUID NOT NULL,
                name                  TEXT NOT NULL,
                subscription_model_id UUID NOT NULL
            );
            """, transaction: transaction);

    public Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        record.Type switch
        {
            nameof(SubscriptionModelAdded) => AddSubscriptionModelAsync(Payload<SubscriptionModelAdded>(record), connection, transaction),
            nameof(TeamAdded) => AddTeamAsync(Payload<TeamAdded>(record), connection, transaction),
            _ => throw new InvalidOperationException($"Projection {Schema} does not handle {record.Type}."),
        };

    private static Task AddSubscriptionModelAsync(SubscriptionModelAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            INSERT INTO tenant_administration.subscription_models (subscription_model_id, name)
            VALUES (@Id, @Name);
            """, e, transaction);

    private static Task AddTeamAsync(TeamAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            INSERT INTO tenant_administration.teams (team_id, tenant_id, name, subscription_model_id)
            VALUES (@Id, @TenantId, @Name, @SubscriptionModelId);
            """, e, transaction);

    private static T Payload<T>(EventRecord record) => JsonSerializer.Deserialize<T>(record.Payload)!;
}
