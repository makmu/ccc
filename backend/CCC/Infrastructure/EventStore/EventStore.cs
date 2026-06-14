using System.Data;
using System.Text.Json;
using Dapper;
using Npgsql;

namespace CCC.Infrastructure.EventStore;

class EventStore(string connectionString)
{
    // The global log has no per-boundary unique constraint, so the conditional
    // append's MAX(position) guard must be evaluated atomically against concurrent
    // writers. SERIALIZABLE makes PostgreSQL's SSI detect two appends racing into
    // the same filtered slice and abort one. A serialization failure can be a false
    // positive, so we retry a bounded number of times before giving up.
    private const int MaxAppendAttempts = 5;

    public async Task AppendAsync(string type, object payload, Guid user)
    {
        var json = JsonSerializer.Serialize(payload);
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.ExecuteAsync(
            """INSERT INTO events (type, payload, "user") VALUES (@type, @payload::jsonb, @user)""",
            new { type, payload = json, user });
    }

    public async Task AppendAsync(string eventType, object eventPayload, Guid user, long expectedMaxPosition, EventFilter contextFilter)
    {
        var json = JsonSerializer.Serialize(eventPayload);
        var (whereClause, parameters) = BuildFilterConditions(contextFilter);

        parameters.Add("type", eventType);
        parameters.Add("payload", json);
        parameters.Add("user", user);
        parameters.Add("expectedMaxPosition", expectedMaxPosition);

        var sql = $"""
            INSERT INTO events (type, payload, "user")
            SELECT @type, @payload::jsonb, @user
            WHERE (
                SELECT COALESCE(MAX(position), -1)
                FROM events
                WHERE {whereClause}
            ) = @expectedMaxPosition
            """;

        var rows = await ExecuteSqlSerializedWithRetriesAsync(sql, parameters);

        if (rows == 0)
            throw new ConcurrencyException("The event store was modified by a concurrent operation.");
    }

    private async Task<int> ExecuteSqlSerializedWithRetriesAsync(string sql, object parameters)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        for (var attempt = 1; ; attempt++)
        {
            await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var rows = await conn.ExecuteAsync(sql, parameters, tx);
                await tx.CommitAsync();
                return rows;
            }
            catch (PostgresException ex) when (IsSerializationFailure(ex))
            {
                await tx.RollbackAsync();

                // Another transaction wrote into the same slice, or SSI flagged a false
                // positive. Either way retry up to the limit; a genuine boundary conflict
                // then surfaces as 0 affected rows to the caller.
                if (attempt >= MaxAppendAttempts)
                    throw new ConcurrencyException("The event store was modified by a concurrent operation.", ex);
            }
        }
    }

    private static bool IsSerializationFailure(PostgresException ex)
        => ex.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected;

    public async Task<IEnumerable<EventRecord>> ReadAsync(long fromPosition, params string[] types)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        return await conn.QueryAsync<EventRecord>(
            """SELECT position, type, payload, timestamp, "user" FROM events WHERE position >= @fromPosition AND type = ANY(@types) ORDER BY position""",
            new { fromPosition, types });
    }

    public async Task<IEnumerable<EventRecord>> LoadAsync(EventFilter filter)
    {
        var (whereClause, parameters) = BuildFilterConditions(filter);

        var sql = $"""
            SELECT position, type, payload, timestamp, "user"
            FROM events
            WHERE {whereClause}
            ORDER BY position
            """;

        await using var conn = new NpgsqlConnection(connectionString);
        return await conn.QueryAsync<EventRecord>(sql, parameters);
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

            if (entry.PayloadProperties.Count == 0)
            {
                conditions.Add($"type = @{typeParam}");
            }
            else
            {
                var kvs = entry.PayloadProperties.ToList();
                var payloadParts = kvs.Select((kv, i) =>
                {
                    var pName = $"p{entryIndex}_{i}";
                    parameters.Add(pName, $"{{\"{kv.Key}\":{kv.Value}}}");
                    return $"payload @> @{pName}::jsonb";
                });
                conditions.Add($"(type = @{typeParam} AND ({string.Join(" OR ", payloadParts)}))");
            }

            entryIndex++;
        }

        return (string.Join(" OR ", conditions), parameters);
    }
}
