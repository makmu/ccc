using CCC.EventStore;
using Npgsql;

namespace CCC.Base.Projections;

public sealed class ProjectionDatabase(string connectionString) : IAsyncDisposable
{
    static ProjectionDatabase() => DapperTypeHandlers.Register();

    private readonly NpgsqlDataSource _dataSource = NpgsqlDataSource.Create(connectionString);

    public ValueTask<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken) =>
        _dataSource.OpenConnectionAsync(cancellationToken);

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();
}
