using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Base.ModuleHosting;
using CCC.EventStore;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CCC.Base.Automations;

// The commands an automation issues append to the event log, while its bookmark lives
// in the module's database, so no transaction covers both. After a crash between
// the two, the runner reacts to the same event again, and the command has to treat
// that as already done.
sealed class AutomationRunner<TAutomation>(
    TAutomation automation,
    IEventStore eventStore,
    ModuleDatabase database,
    AppendSignal signal,
    RunnerRetryPolicy retryPolicy,
    ILogger<AutomationRunner<TAutomation>> logger) : EventLogRunner(eventStore, database, signal, retryPolicy, logger)
    where TAutomation : IAutomation
{
    protected override string Kind => "Automation";

    protected override string Schema => automation.Schema;

    protected override IReadOnlyCollection<string> EventTypes => automation.Handlers.EventTypes;

    protected override async Task<(long Position, bool Stalled)> InstallAsync(CancellationToken cancellationToken)
    {
        await using var connection = await Database.OpenAsync(cancellationToken);
        var bookmark = await connection.QuerySingleAsync<Bookmark>(Install().With(cancellationToken));
        return (bookmark.Position, bookmark.Stalled);
    }

    protected override async Task ProcessBatchAsync(IReadOnlyList<EventRecord> batch, CancellationToken cancellationToken)
    {
        foreach (var record in batch)
        {
            await ReactAsync(record, cancellationToken);

            await using var connection = await Database.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(MoveBookmark(record.Position).With(cancellationToken));
            Position = record.Position;
        }
    }

    private async Task ReactAsync(EventRecord record, CancellationToken cancellationToken)
    {
        CommandResult result;
        try
        {
            result = await automation.Handlers.Handle(record)(cancellationToken);
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or NpgsqlException { IsTransient: true }))
        {
            throw new InvalidOperationException($"Reacting to event {record.Position} ({record.Type}) failed: {ex.Message}", ex);
        }

        if (result.Error is { } error)
            throw new InvalidOperationException(
                $"Reacting to event {record.Position} ({record.Type}) failed: {error.Kind}: {error.Message}");
    }

    private CommandDefinition Install() =>
        new($"""
            CREATE SCHEMA IF NOT EXISTS {automation.Schema};
            CREATE TABLE IF NOT EXISTS {automation.Schema}.bookmark (
                position     BIGINT      NOT NULL,
                stalled_at   TIMESTAMPTZ,
                stall_reason TEXT
            );
            INSERT INTO {automation.Schema}.bookmark (position)
            SELECT 0 WHERE NOT EXISTS (SELECT FROM {automation.Schema}.bookmark);
            SELECT position, stalled_at IS NOT NULL AS stalled FROM {automation.Schema}.bookmark;
            """);

    private sealed record Bookmark(long Position, bool Stalled);
}
