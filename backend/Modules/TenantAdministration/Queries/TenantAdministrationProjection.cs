using CCC.Base.Dcb;
using CCC.Base.ModuleHosting;
using CCC.Base.Projections;
using CCC.Domain.Events;
using Dapper;
using Npgsql;

namespace CCC.Modules.TenantAdministration.Queries;

sealed class TenantAdministrationProjection : IProjection
{
    public string Schema => "tenant_administration";

    public int Version => 1;

    public EventHandlers<CommandDefinition> Handlers { get; } = new EventHandlers<CommandDefinition>()
        .On<SubscriptionModelAdded>(AddSubscriptionModel)
        .On<TeamAdded>(AddTeam);

    public Task InstallAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(InstallTables().With(transaction, cancellationToken));

    public Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(Handlers.Handle(record).With(transaction, cancellationToken));

    private static CommandDefinition InstallTables() =>
        new("""
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
            """);

    private static CommandDefinition AddSubscriptionModel(SubscriptionModelAdded e) =>
        new("""
            INSERT INTO tenant_administration.subscription_models (subscription_model_id, name)
            VALUES (@Id, @Name);
            """, e);

    private static CommandDefinition AddTeam(TeamAdded e) =>
        new("""
            INSERT INTO tenant_administration.teams (team_id, tenant_id, name, subscription_model_id)
            VALUES (@Id, @TenantId, @Name, @SubscriptionModelId);
            """, e);
}
