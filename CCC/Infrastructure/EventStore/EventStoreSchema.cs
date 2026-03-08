using Dapper;
using Npgsql;

namespace CCC.Infrastructure.EventStore;

static class EventStoreSchema
{
    public static async Task CreateAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS events (
                position  BIGSERIAL PRIMARY KEY,
                type      TEXT        NOT NULL,
                payload   JSONB       NOT NULL,
                timestamp TIMESTAMPTZ NOT NULL DEFAULT now(),
                "user"    UUID        NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_events_type    ON events (type);
            CREATE INDEX IF NOT EXISTS idx_events_payload ON events USING GIN (payload);
            """);
    }
}
