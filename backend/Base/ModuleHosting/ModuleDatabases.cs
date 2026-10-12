using System.Collections.Concurrent;
using CCC.EventStore;
using Npgsql;

namespace CCC.Base.ModuleHosting;

public sealed class ModuleDatabases(Func<string, string> connectionStringOf) : IAsyncDisposable
{
    static ModuleDatabases() => DapperTypeHandlers.Register();

    // GetOrAdd may run its factory twice for the same key. The Lazy makes sure only
    // one NpgsqlDataSource is ever created per connection string, so none leaks.
    private readonly ConcurrentDictionary<string, Lazy<NpgsqlDataSource>> _dataSources = new();

    internal NpgsqlDataSource DataSourceOf(string module) =>
        _dataSources.GetOrAdd(connectionStringOf(module), connectionString => new(() => NpgsqlDataSource.Create(connectionString))).Value;

    public async ValueTask DisposeAsync()
    {
        foreach (var dataSource in _dataSources.Values.Where(dataSource => dataSource.IsValueCreated))
            await dataSource.Value.DisposeAsync();
    }
}
