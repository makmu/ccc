using System.Threading.Channels;
using CCC.Base.Dcb;
using CCC.EventStore;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CCC.Base.Projections;

// Exactly one runner writes a projection's schema, so neither the bookmark nor the
// projection tables need locking. Running the API twice would break this.
sealed class ProjectionRunner<TProjection>(
    TProjection projection,
    IEventStore eventStore,
    ProjectionDatabase database,
    AppendSignal signal,
    ProjectionRetryPolicy retryPolicy,
    ILogger<ProjectionRunner<TProjection>> logger) : BackgroundService
    where TProjection : IProjection
{
    private const int BatchSize = 100;

    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(1);

    private readonly ChannelReader<bool> _appends = signal.Subscribe();
    private long _position;
    private bool _stalled;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (await RetryAsync(InstallAsync, stoppingToken) is { } installFailure)
        {
            logger.LogCritical(installFailure, "Projection {Schema} could not be installed and gives up until the next start",
                projection.Schema);
            return;
        }

        if (_stalled)
        {
            logger.LogWarning("Projection {Schema} is stalled after position {Position} and does not catch up",
                projection.Schema, _position);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (await RetryAsync(CatchUpAsync, stoppingToken) is { } failure)
            {
                await StallAsync(failure, stoppingToken);
                return;
            }

            await WaitForAppendAsync(stoppingToken);
        }
    }

    // Returns the last failure once the retries are used up. Transient database
    // failures don't use up retries, since they can't be recorded as a stall anyway.
    private async Task<Exception?> RetryAsync(Func<CancellationToken, Task> step, CancellationToken stoppingToken)
    {
        var retries = 0;
        for (var failures = 1; ; failures++)
        {
            try
            {
                await step(stoppingToken);
                return null;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                if (ex is not NpgsqlException { IsTransient: true } && retries++ == retryPolicy.MaxRetries)
                    return ex;

                var backoff = TimeSpan.FromTicks((long)Math.Min(
                    retryPolicy.FirstDelay.Ticks * Math.Pow(2, failures - 1), MaxBackoff.Ticks));
                logger.LogError(ex, "Projection {Schema} failed after position {Position}, retrying in {Backoff}",
                    projection.Schema, _position, backoff);
                await Task.Delay(backoff, stoppingToken);
            }
        }
    }

    private async Task InstallAsync(CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(EnsureBookmark().With(transaction, cancellationToken));
        var bookmark = await connection.QuerySingleOrDefaultAsync<Bookmark>(LoadBookmark().With(transaction, cancellationToken));

        if (bookmark?.Version != projection.Version)
        {
            logger.LogInformation("Rebuilding projection {Schema} as version {Version}", projection.Schema, projection.Version);
            bookmark = new Bookmark(projection.Version, 0, false);
            await connection.ExecuteAsync(Rebuild(bookmark).With(transaction, cancellationToken));
        }

        await projection.InstallAsync(connection, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        _position = bookmark.Position;
        _stalled = bookmark.Stalled;
    }

    private async Task CatchUpAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var batch = (await eventStore.ReadAsync(_position + 1, BatchSize, [.. projection.Handlers.EventTypes], cancellationToken)).ToList();
            if (batch.Count == 0)
                return;

            await using var connection = await database.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            foreach (var record in batch)
                await ApplyAsync(record, connection, transaction, cancellationToken);

            var position = batch[^1].Position;
            await connection.ExecuteAsync(MoveBookmark(position).With(transaction, cancellationToken));
            await transaction.CommitAsync(cancellationToken);
            _position = position;

            if (batch.Count < BatchSize)
                return;
        }
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

    private async Task StallAsync(Exception failure, CancellationToken stoppingToken)
    {
        logger.LogCritical(failure, "Projection {Schema} stalls after position {Position}", projection.Schema, _position);

        var recordFailure = await RetryAsync(async cancellationToken =>
        {
            await using var connection = await database.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(RecordStall(failure.Message).With(cancellationToken));
        }, stoppingToken);

        if (recordFailure is not null)
            logger.LogCritical(recordFailure, "Projection {Schema} could not record its stall and catches up again on the next start",
                projection.Schema);
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

    private CommandDefinition MoveBookmark(long position) =>
        new($"UPDATE {projection.Schema}.bookmark SET position = @position", new { position });

    private CommandDefinition RecordStall(string reason) =>
        new($"UPDATE {projection.Schema}.bookmark SET stalled_at = now(), stall_reason = @reason", new { reason });

    private string BookmarkTable => $"""
        CREATE TABLE IF NOT EXISTS {projection.Schema}.bookmark (
            version      INT         NOT NULL,
            position     BIGINT      NOT NULL,
            stalled_at   TIMESTAMPTZ,
            stall_reason TEXT
        );
        """;

    private async Task WaitForAppendAsync(CancellationToken stoppingToken)
    {
        await _appends.WaitToReadAsync(stoppingToken);
        _appends.TryRead(out _);
    }

    private sealed record Bookmark(int Version, long Position, bool Stalled);
}
