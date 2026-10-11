using CCC.EventStore;
using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace CCC.Base.Projections;

sealed class ProjectionHealthCheck<TProjection>(TProjection projection, IEventStore eventStore, ProjectionDatabase database)
    : IHealthCheck
    where TProjection : IProjection
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        var bookmark = await LoadBookmarkAsync(cancellationToken);
        if (bookmark is null)
            return HealthCheckResult.Unhealthy("Not installed yet, so its queries fail.");

        var data = new Dictionary<string, object>
        {
            ["version"] = bookmark.Version,
            ["position"] = bookmark.Position,
        };

        if (bookmark.Version != projection.Version)
            return HealthCheckResult.Unhealthy(
                $"Version {projection.Version} is not installed, so its queries answer from version {bookmark.Version}.", data: data);

        if (bookmark.StalledAt is { } stalledAt)
            return HealthCheckResult.Unhealthy(
                $"Stalled at {stalledAt:O} after position {bookmark.Position}: {bookmark.StallReason}", data: data);

        var pending = await eventStore.ReadAsync(bookmark.Position + 1, 1, [.. projection.Handlers.EventTypes], cancellationToken);
        if (pending.Any())
            return HealthCheckResult.Degraded($"Catching up after position {bookmark.Position}.", data: data);

        return HealthCheckResult.Healthy($"Caught up at position {bookmark.Position}.", data);
    }

    private async Task<Bookmark?> LoadBookmarkAsync(CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        try
        {
            return await connection.QuerySingleOrDefaultAsync<Bookmark>(new CommandDefinition(
                $"SELECT version, position, stalled_at AS stalledAt, stall_reason AS stallReason FROM {projection.Schema}.bookmark",
                cancellationToken: cancellationToken));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return null;
        }
    }

    private sealed record Bookmark(int Version, long Position, DateTime? StalledAt, string? StallReason);
}
