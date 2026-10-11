using CCC.EventStore;
using Dapper;
using Npgsql;

namespace CCC.ProjectionBase;

public interface IProjection
{
    // Each projection owns one schema. Raising the version drops the schema and
    // rebuilds the projection from the start of the event log.
    string Schema { get; }

    int Version { get; }

    EventHandlers<CommandDefinition> Handlers { get; }

    Task InstallAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);

    Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);
}
