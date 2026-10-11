using Npgsql;

namespace CCC.EventStore;

sealed class EventStoreDatabase(string connectionString) : IAsyncDisposable
{
    static EventStoreDatabase() => DapperTypeHandlers.Register();

    private readonly NpgsqlDataSource _dataSource = NpgsqlDataSource.Create(connectionString);

    public ValueTask<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken) =>
        _dataSource.OpenConnectionAsync(cancellationToken);

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();
}
