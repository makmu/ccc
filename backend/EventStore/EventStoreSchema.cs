using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.EventStore;

public static class EventStoreSchema
{
    public static async Task CreateAsync(IServiceProvider services)
    {
        await using var conn = await services.GetRequiredService<EventStoreDatabase>().OpenAsync(CancellationToken.None);
        await conn.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS events (
                position  BIGSERIAL PRIMARY KEY,
                type      TEXT        NOT NULL,
                payload   JSONB       NOT NULL,
                timestamp TIMESTAMPTZ NOT NULL DEFAULT now(),
                "user"    UUID        NOT NULL
            );

            ALTER TABLE events ADD COLUMN IF NOT EXISTS tags TEXT[] NOT NULL DEFAULT '{}';

            CREATE INDEX IF NOT EXISTS idx_events_type    ON events (type);
            CREATE INDEX IF NOT EXISTS idx_events_tags    ON events USING GIN (tags);

            -- For reading the log by tag. It can't use idx_events_tags, so the event store keeps filtering events directly.
            CREATE OR REPLACE VIEW event_view AS
            SELECT e.position,
                   t.tag,
                   split_part(t.tag, '=', 1)                        AS tag_key,
                   substring(t.tag FROM position('=' IN t.tag) + 1) AS tag_value,
                   e.type,
                   e.payload,
                   e.timestamp,
                   e."user"
            FROM events e
            CROSS JOIN LATERAL unnest(e.tags) AS t(tag);
            """);
    }
}
