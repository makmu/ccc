using System.Text.Json;
using Dapper;
using Npgsql;

namespace CCC.Infrastructure.EventStore;

class EventStore(string connectionString)
{
    public async Task AppendAsync(string type, object payload, Guid user)
    {
        var json = JsonSerializer.Serialize(payload);
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.ExecuteAsync(
            """INSERT INTO events (type, payload, "user") VALUES (@type, @payload::jsonb, @user)""",
            new { type, payload = json, user });
    }

    public async Task<IEnumerable<EventRecord>> ReadAsync(long fromPosition, params string[] types)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        return await conn.QueryAsync<EventRecord>(
            """SELECT position, type, payload, timestamp, "user" FROM events WHERE position >= @fromPosition AND type = ANY(@types) ORDER BY position""",
            new { fromPosition, types });
    }

    public async Task<IEnumerable<EventRecord>> LoadAsync(EventFilter filter)
    {
        var kvList = filter.PayloadProperties.ToList();

        var payloadClause = kvList.Count == 0
            ? ""
            : " AND (" + string.Join(" OR ", kvList.Select((_, i) => $"payload @> @p{i}::jsonb")) + ")";

        var sql = $"""
            SELECT position, type, payload, timestamp, "user"
            FROM events
            WHERE type = ANY(@types){payloadClause}
            ORDER BY position
            """;

        var parameters = new DynamicParameters();
        parameters.Add("types", filter.Types);
        for (var i = 0; i < kvList.Count; i++)
            parameters.Add($"p{i}", JsonSerializer.Serialize(new Dictionary<string, string> { { kvList[i].Key, kvList[i].Value } }));

        await using var conn = new NpgsqlConnection(connectionString);
        return await conn.QueryAsync<EventRecord>(sql, parameters);
    }
}
