using System.Threading.Channels;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CCC.EventStore;

// Exactly one runner writes a projection's schema, so neither the bookmark nor the
// read model tables need locking. Running the API twice would break this.
sealed class ProjectionRunner<TProjection>(
    TProjection projection,
    IEventStore eventStore,
    ReadModelDatabase database,
    AppendSignal signal,
    ILogger<ProjectionRunner<TProjection>> logger) : BackgroundService
    where TProjection : IProjection
{
    private const int BatchSize = 100;

    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(1);

    private readonly ChannelReader<bool> _appends = signal.Subscribe();
    private long _position;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RetryAsync(InstallAsync, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RetryAsync(CatchUpAsync, stoppingToken);
            await WaitForAppendAsync(stoppingToken);
        }
    }

    private async Task RetryAsync(Func<CancellationToken, Task> step, CancellationToken stoppingToken)
    {
        for (var failures = 1; ; failures++)
        {
            try
            {
                await step(stoppingToken);
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                var backoff = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, failures), MaxBackoff.TotalSeconds));
                logger.LogError(ex, "Projection {Schema} failed after position {Position}, retrying in {Backoff}",
                    projection.Schema, _position, backoff);
                await Task.Delay(backoff, stoppingToken);
            }
        }
    }

    private async Task InstallAsync(CancellationToken cancellationToken)
    {
        var schema = projection.Schema;
        await using var connection = await database.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync($"""
            CREATE SCHEMA IF NOT EXISTS {schema};
            CREATE TABLE IF NOT EXISTS {schema}.bookmark (
                version  INT    NOT NULL,
                position BIGINT NOT NULL
            );
            """, transaction: transaction);

        var bookmark = await connection.QuerySingleOrDefaultAsync<Bookmark>(
            $"SELECT version, position FROM {schema}.bookmark", transaction: transaction);

        if (bookmark?.Version != projection.Version)
        {
            logger.LogInformation("Rebuilding projection {Schema} as version {Version}", schema, projection.Version);
            bookmark = new Bookmark(projection.Version, 0);
            await connection.ExecuteAsync($"""
                DROP SCHEMA {schema} CASCADE;
                CREATE SCHEMA {schema};
                CREATE TABLE {schema}.bookmark (
                    version  INT    NOT NULL,
                    position BIGINT NOT NULL
                );
                INSERT INTO {schema}.bookmark (version, position) VALUES (@Version, @Position);
                """, bookmark, transaction);
        }

        await projection.InstallAsync(connection, transaction);
        await transaction.CommitAsync(cancellationToken);
        _position = bookmark.Position;
    }

    private async Task CatchUpAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var batch = (await eventStore.ReadAsync(_position + 1, BatchSize, [.. projection.EventTypes])).ToList();
            if (batch.Count == 0)
                return;

            await using var connection = await database.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            foreach (var record in batch)
                await projection.ApplyAsync(record, connection, transaction);

            var position = batch[^1].Position;
            await connection.ExecuteAsync(
                $"UPDATE {projection.Schema}.bookmark SET position = @position", new { position }, transaction);
            await transaction.CommitAsync(cancellationToken);
            _position = position;

            if (batch.Count < BatchSize)
                return;
        }
    }

    private async Task WaitForAppendAsync(CancellationToken stoppingToken)
    {
        await _appends.WaitToReadAsync(stoppingToken);
        _appends.TryRead(out _);
    }

    private sealed record Bookmark(int Version, long Position);
}
