using Npgsql;

namespace CCC.EventStore;

public interface IProjection
{
    // Each projection owns one schema. Raising the version drops the schema and
    // rebuilds the read model from the start of the event log.
    string Schema { get; }

    int Version { get; }

    IReadOnlyCollection<string> EventTypes { get; }

    Task InstallAsync(NpgsqlConnection connection, NpgsqlTransaction transaction);

    Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction);
}
