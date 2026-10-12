using CCC.Base.Dcb;
using CCC.EventStore;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CCC.Base.Projections;

sealed class ProjectionRunner<TProjection>(
    TProjection projection,
    IEventStore eventStore,
    ProjectionDatabase database,
    AppendSignal signal,
    ProjectionRetryPolicy retryPolicy,
    ILogger<ProjectionRunner<TProjection>> logger) : EventLogRunner(eventStore, database, signal, retryPolicy, logger)
    where TProjection : IProjection
{
    protected override string Kind => "Projection";

    protected override string Schema => projection.Schema;

    protected override IReadOnlyCollection<string> EventTypes => projection.Handlers.EventTypes;

    protected override async Task<(long Position, bool Stalled)> InstallAsync(CancellationToken cancellationToken)
    {
        await using var connection = await Database.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(EnsureBookmark().With(transaction, cancellationToken));
        var bookmark = await connection.QuerySingleOrDefaultAsync<Bookmark>(LoadBookmark().With(transaction, cancellationToken));

        if (bookmark?.Version != projection.Version)
        {
            Logger.LogInformation("Rebuilding projection {Schema} as version {Version}", projection.Schema, projection.Version);
            bookmark = new Bookmark(projection.Version, 0, false);
            await connection.ExecuteAsync(Rebuild(bookmark).With(transaction, cancellationToken));
        }

        await projection.InstallAsync(connection, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (bookmark.Position, bookmark.Stalled);
    }

    protected override async Task ProcessBatchAsync(IReadOnlyList<EventRecord> batch, CancellationToken cancellationToken)
    {
        await using var connection = await Database.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var record in batch)
            await ApplyAsync(record, connection, transaction, cancellationToken);

        var position = batch[^1].Position;
        await connection.ExecuteAsync(MoveBookmark(position).With(transaction, cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        Position = position;
    }

    private async Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await projection.ApplyAsync(record, connection, transaction, cancellationToken);
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or NpgsqlException { IsTransient: true }))
        {
            throw new InvalidOperationException($"Applying event {record.Position} ({record.Type}) failed: {ex.Message}", ex);
        }
    }

    private CommandDefinition EnsureBookmark() =>
        new($"""
            CREATE SCHEMA IF NOT EXISTS {projection.Schema};
            {BookmarkTable}
            """);

    private CommandDefinition LoadBookmark() =>
        new($"SELECT version, position, stalled_at IS NOT NULL AS stalled FROM {projection.Schema}.bookmark");

    private CommandDefinition Rebuild(Bookmark bookmark) =>
        new($"""
            DROP SCHEMA {projection.Schema} CASCADE;
            CREATE SCHEMA {projection.Schema};
            {BookmarkTable}
            INSERT INTO {projection.Schema}.bookmark (version, position) VALUES (@Version, @Position);
            """, bookmark);

    private string BookmarkTable => $"""
        CREATE TABLE IF NOT EXISTS {projection.Schema}.bookmark (
            version      INT         NOT NULL,
            position     BIGINT      NOT NULL,
            stalled_at   TIMESTAMPTZ,
            stall_reason TEXT
        );
        """;

    private sealed record Bookmark(int Version, long Position, bool Stalled);
}
