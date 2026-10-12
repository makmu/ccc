using System.Threading.Channels;
using CCC.Base.Dcb;
using CCC.EventStore;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CCC.Base.ModuleHosting;

// Follows the event log from a bookmark in the module's database. Exactly one runner
// writes a schema, so neither the bookmark nor the schema's tables need locking.
// Running the API twice would break this.
public abstract class EventLogRunner(
    IEventStore eventStore,
    ModuleDatabase database,
    AppendSignal signal,
    RunnerRetryPolicy retryPolicy,
    ILogger logger) : BackgroundService
{
    private const int BatchSize = 100;

    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(1);

    private readonly ChannelReader<bool> _appends = signal.Subscribe();
    private bool _stalled;

    protected ModuleDatabase Database { get; } = database;

    protected ILogger Logger { get; } = logger;

    // Moved by ProcessBatchAsync once a bookmark is committed, so that a retry
    // continues after the last event whose effects are bookmarked.
    protected long Position { get; set; }

    protected abstract string Kind { get; }

    protected abstract string Schema { get; }

    protected abstract IReadOnlyCollection<string> EventTypes { get; }

    protected abstract Task<(long Position, bool Stalled)> InstallAsync(CancellationToken cancellationToken);

    protected abstract Task ProcessBatchAsync(IReadOnlyList<EventRecord> batch, CancellationToken cancellationToken);

    protected CommandDefinition MoveBookmark(long position) =>
        new($"UPDATE {Schema}.bookmark SET position = @position", new { position });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (await RetryAsync(InstallBookmarkAsync, stoppingToken) is { } installFailure)
        {
            Logger.LogCritical(installFailure, "{Kind} {Schema} could not be installed and gives up until the next start",
                Kind, Schema);
            return;
        }

        if (_stalled)
        {
            Logger.LogWarning("{Kind} {Schema} is stalled after position {Position} and does not catch up",
                Kind, Schema, Position);
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
                Logger.LogError(ex, "{Kind} {Schema} failed after position {Position}, retrying in {Backoff}",
                    Kind, Schema, Position, backoff);
                await Task.Delay(backoff, stoppingToken);
            }
        }
    }

    private async Task InstallBookmarkAsync(CancellationToken cancellationToken) =>
        (Position, _stalled) = await InstallAsync(cancellationToken);

    private async Task CatchUpAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var batch = (await eventStore.ReadAsync(Position + 1, BatchSize, [.. EventTypes], cancellationToken)).ToList();
            if (batch.Count == 0)
                return;

            await ProcessBatchAsync(batch, cancellationToken);

            if (batch.Count < BatchSize)
                return;
        }
    }

    private async Task StallAsync(Exception failure, CancellationToken stoppingToken)
    {
        Logger.LogCritical(failure, "{Kind} {Schema} stalls after position {Position}", Kind, Schema, Position);

        var recordFailure = await RetryAsync(async cancellationToken =>
        {
            await using var connection = await Database.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(RecordStall(failure.Message).With(cancellationToken));
        }, stoppingToken);

        if (recordFailure is not null)
            Logger.LogCritical(recordFailure, "{Kind} {Schema} could not record its stall and catches up again on the next start",
                Kind, Schema);
    }

    private CommandDefinition RecordStall(string reason) =>
        new($"UPDATE {Schema}.bookmark SET stalled_at = now(), stall_reason = @reason", new { reason });

    private async Task WaitForAppendAsync(CancellationToken stoppingToken)
    {
        await _appends.WaitToReadAsync(stoppingToken);
        _appends.TryRead(out _);
    }
}
