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
        connection.ExecuteAsync(new CommandDefinition("""
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
            """, transaction: transaction, cancellationToken: cancellationToken));

    public Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        record.Type switch
        {
            nameof(SubscriptionModelAdded) => AddSubscriptionModelAsync(record.PayloadAs<SubscriptionModelAdded>(), connection, transaction, cancellationToken),
            nameof(TenantRegistered) => RegisterTenantAsync(record.PayloadAs<TenantRegistered>(), connection, transaction, cancellationToken),
            nameof(TenantRenamed) => RenameTenantAsync(record.PayloadAs<TenantRenamed>(), connection, transaction, cancellationToken),
            nameof(TenantOwnerAssigned) => AssignTenantOwnerAsync(record.PayloadAs<TenantOwnerAssigned>(), connection, transaction, cancellationToken),
            nameof(UserAdded) => AddUserAsync(record.PayloadAs<UserAdded>(), connection, transaction, cancellationToken),
            _ => throw new InvalidOperationException($"Projection {Schema} does not handle {record.Type}."),
        };

    private static Task AddSubscriptionModelAsync(SubscriptionModelAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO app_administration.subscription_models (subscription_model_id, name, key_limit)
            VALUES (@Id, @Name, @KeyLimit);
            """, e, transaction, cancellationToken: cancellationToken));

    private static async Task RegisterTenantAsync(TenantRegistered e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO app_administration.tenants (tenant_id, name)
            VALUES (@Id, @Name);
            """, e, transaction, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO app_administration.tenant_subscription_models (tenant_id, subscription_model_id)
            VALUES (@TenantId, @SubscriptionModelId);
            """, e.SubscriptionModelIds.Select(id => new { TenantId = e.Id, SubscriptionModelId = id }),
            transaction, cancellationToken: cancellationToken));
    }

    private static Task RenameTenantAsync(TenantRenamed e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            UPDATE app_administration.tenants SET name = @Name WHERE tenant_id = @TenantId;
            """, e, transaction, cancellationToken: cancellationToken));

    private static Task AssignTenantOwnerAsync(TenantOwnerAssigned e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO app_administration.tenant_owners (tenant_id, user_id)
            VALUES (@TenantId, @UserId);
            """, e, transaction, cancellationToken: cancellationToken));

    private static Task AddUserAsync(UserAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO app_administration.users (user_id, name, email)
            VALUES (@Id, @Name, @Email);
            """, e, transaction, cancellationToken: cancellationToken));
}
