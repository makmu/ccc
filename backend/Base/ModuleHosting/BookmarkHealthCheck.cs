using CCC.EventStore;
using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace CCC.Base.ModuleHosting;

public abstract class BookmarkHealthCheck(IEventStore eventStore, ModuleDatabase database) : IHealthCheck
{
    protected abstract string Schema { get; }

    protected abstract IReadOnlyCollection<string> EventTypes { get; }

    protected abstract string NotInstalledDescription { get; }

    public virtual async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        var bookmark = await QueryBookmarkAsync<Bookmark>(
            "position, stalled_at AS stalledAt, stall_reason AS stallReason", cancellationToken);
        if (bookmark is null)
            return HealthCheckResult.Unhealthy(NotInstalledDescription);

        var data = new Dictionary<string, object> { ["position"] = bookmark.Position };

        if (bookmark.StalledAt is { } stalledAt)
            return HealthCheckResult.Unhealthy(
                $"Stalled at {stalledAt:O} after position {bookmark.Position}: {bookmark.StallReason}", data: data);

        var pending = await eventStore.ReadAsync(bookmark.Position + 1, 1, [.. EventTypes], cancellationToken);
        if (pending.Any())
            return HealthCheckResult.Degraded($"Catching up after position {bookmark.Position}.", data: data);

        return HealthCheckResult.Healthy($"Caught up at position {bookmark.Position}.", data);
    }

    protected async Task<T?> QueryBookmarkAsync<T>(string columns, CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        try
        {
            return await connection.QuerySingleOrDefaultAsync<T>(new CommandDefinition(
                $"SELECT {columns} FROM {Schema}.bookmark", cancellationToken: cancellationToken));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return default;
        }
    }

    private sealed record Bookmark(long Position, DateTime? StalledAt, string? StallReason);
}
