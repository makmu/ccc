using System.Data;
using System.Text.Json;
using CCC.Dcb;
using Dapper;
using Npgsql;

namespace CCC.EventStore;

class PostgresEventStore(string connectionString, AppendSignal signal) : IEventStore
{
    // Positions come from a sequence and are taken before commit, so two concurrent
    // appends could commit out of position order, and a reader that tracks the last
    // position it saw would skip the earlier one for good. Every append holds this
    // lock from INSERT to COMMIT, which makes positions visible strictly in order.
    // Under the lock each statement's snapshot also includes every earlier append,
    // so the conditional append's MAX(position) check is exact in READ COMMITTED.
    private const long AppendLockKey = 0x4343435F4556;

    public async Task AppendAsync(string type, object payload, Guid user, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        await ExecuteUnderAppendLockAsync(
            """INSERT INTO events (type, payload, "user") VALUES (@type, @payload::jsonb, @user)""",
            new { type, payload = json, user }, cancellationToken);
    }

    public async Task AppendAsync(string eventType, object eventPayload, Guid user, long expectedMaxPosition, EventFilter contextFilter, string[] tags,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(eventPayload);
        var (whereClause, parameters) = BuildFilterConditions(contextFilter);

        parameters.Add("type", eventType);
        parameters.Add("payload", json);
        parameters.Add("user", user);
        parameters.Add("tags", tags);
        parameters.Add("expectedMaxPosition", expectedMaxPosition);

        var sql = $"""
            INSERT INTO events (type, payload, "user", tags)
            SELECT @type, @payload::jsonb, @user, @tags
            WHERE (
                SELECT COALESCE(MAX(position), -1)
                FROM events
                WHERE {whereClause}
            ) = @expectedMaxPosition
            """;

        var rows = await ExecuteUnderAppendLockAsync(sql, parameters, cancellationToken);

        if (rows == 0)
            throw new ConcurrencyException("The event store was modified by a concurrent operation.");
    }

    private async Task<int> ExecuteUnderAppendLockAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        await conn.ExecuteAsync(new CommandDefinition(
            "SELECT pg_advisory_xact_lock(@AppendLockKey)", new { AppendLockKey }, tx, cancellationToken: cancellationToken));
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, parameters, tx, cancellationToken: cancellationToken));

        // Not cancellable: a commit that succeeds on the server but reports a cancellation
        // would skip the signal, and projections would not see the event until the next append.
        await tx.CommitAsync();

        if (rows > 0)
            signal.Notify();
        return rows;
    }

    public async Task<IEnumerable<EventRecord>> ReadAsync(long fromPosition, int limit, string[] types, CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        return await conn.QueryAsync<EventRecord>(new CommandDefinition(
            """SELECT position, type, payload, timestamp, "user", tags FROM events WHERE position >= @fromPosition AND type = ANY(@types) ORDER BY position LIMIT @limit""",
            new { fromPosition, limit, types }, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<EventRecord>> LoadAsync(EventFilter filter, CancellationToken cancellationToken)
    {
        var (whereClause, parameters) = BuildFilterConditions(filter);

        var sql = $"""
            SELECT position, type, payload, timestamp, "user", tags
            FROM events
            WHERE {whereClause}
            ORDER BY position
            """;

        await using var conn = new NpgsqlConnection(connectionString);
        return await conn.QueryAsync<EventRecord>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    private static (string WhereClause, DynamicParameters Parameters) BuildFilterConditions(EventFilter filter)
    {
        var parameters = new DynamicParameters();
        var conditions = new List<string>();
        var entryIndex = 0;

        foreach (var entry in filter.Entries)
        {
            var typeParam = $"t{entryIndex}";
            parameters.Add(typeParam, entry.Type);

            var parts = new List<string> { $"type = @{typeParam}" };

            if (entry.Tags.Count > 0)
            {
                var tagParam = $"tags{entryIndex}";
                parameters.Add(tagParam, entry.Tags.ToArray());
                parts.Add($"tags @> @{tagParam}");
            }

            conditions.Add($"({string.Join(" AND ", parts)})");
            entryIndex++;
        }

        return (string.Join(" OR ", conditions), parameters);
    }
}
