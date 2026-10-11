using CCC.Domain.Events;
using CCC.EventStore;
using Dapper;
using Npgsql;

namespace CCC.Modules.AppAdministration.Queries;

sealed class AppAdministrationProjection : IProjection
{
    public string Schema => "app_administration";

    public int Version => 1;

    public IReadOnlyCollection<string> EventTypes { get; } =
    [
        nameof(SubscriptionModelAdded),
        nameof(TenantRegistered),
        nameof(TenantRenamed),
        nameof(TenantOwnerAssigned),
        nameof(UserAdded),
    ];

    public Task InstallAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(InstallTables().With(transaction, cancellationToken));

    public Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var command = record.Type switch
        {
            nameof(SubscriptionModelAdded) => AddSubscriptionModel(record.PayloadAs<SubscriptionModelAdded>()),
            nameof(TenantRegistered) => RegisterTenant(record.PayloadAs<TenantRegistered>()),
            nameof(TenantRenamed) => RenameTenant(record.PayloadAs<TenantRenamed>()),
            nameof(TenantOwnerAssigned) => AssignTenantOwner(record.PayloadAs<TenantOwnerAssigned>()),
            nameof(UserAdded) => AddUser(record.PayloadAs<UserAdded>()),
            _ => throw new InvalidOperationException($"Projection {Schema} does not handle {record.Type}."),
        };
        return connection.ExecuteAsync(command.With(transaction, cancellationToken));
    }

    private static CommandDefinition InstallTables() =>
        new("""
            CREATE TABLE IF NOT EXISTS app_administration.subscription_models (
                subscription_model_id UUID PRIMARY KEY,
                name                  TEXT NOT NULL,
                key_limit             INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS app_administration.tenants (
                tenant_id UUID PRIMARY KEY,
                name      TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS app_administration.tenant_subscription_models (
                tenant_id             UUID NOT NULL,
                subscription_model_id UUID NOT NULL,
                PRIMARY KEY (tenant_id, subscription_model_id)
            );

            CREATE TABLE IF NOT EXISTS app_administration.tenant_owners (
                tenant_id UUID NOT NULL,
                user_id   UUID NOT NULL,
                PRIMARY KEY (tenant_id, user_id)
            );

            CREATE TABLE IF NOT EXISTS app_administration.users (
                user_id UUID PRIMARY KEY,
                name    TEXT NOT NULL,
                email   TEXT NOT NULL
            );
            """);

    private static CommandDefinition AddSubscriptionModel(SubscriptionModelAdded e) =>
        new("""
            INSERT INTO app_administration.subscription_models (subscription_model_id, name, key_limit)
            VALUES (@Id, @Name, @KeyLimit);
            """, e);

    private static CommandDefinition RegisterTenant(TenantRegistered e) =>
        new("""
            INSERT INTO app_administration.tenants (tenant_id, name)
            VALUES (@Id, @Name);

            INSERT INTO app_administration.tenant_subscription_models (tenant_id, subscription_model_id)
            SELECT @Id, unnest(@SubscriptionModelIds);
            """, e);

    private static CommandDefinition RenameTenant(TenantRenamed e) =>
        new("""
            UPDATE app_administration.tenants SET name = @Name WHERE tenant_id = @TenantId;
            """, e);

    private static CommandDefinition AssignTenantOwner(TenantOwnerAssigned e) =>
        new("""
            INSERT INTO app_administration.tenant_owners (tenant_id, user_id)
            VALUES (@TenantId, @UserId);
            """, e);

    private static CommandDefinition AddUser(UserAdded e) =>
        new("""
            INSERT INTO app_administration.users (user_id, name, email)
            VALUES (@Id, @Name, @Email);
            """, e);
}
