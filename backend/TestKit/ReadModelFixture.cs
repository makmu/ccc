using CCC.Abstractions;
using CCC.EventStore;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace CCC.TestKit;

// Runs a module's projection against a throwaway database on the server given by
// CCC_TEST_POSTGRES, defaulting to the local development server.
public sealed class ReadModelFixture(string schema, Func<IServiceCollection, IServiceCollection> addModule)
{
    private static readonly TimeSpan CatchUpTimeout = TimeSpan.FromSeconds(5);

    private static readonly string ServerConnectionString =
        Environment.GetEnvironmentVariable("CCC_TEST_POSTGRES")
        ?? "Host=localhost:5432;Username=postgres;Password=mypass";

    private readonly string _database = $"ccc_test_{Guid.NewGuid():N}";
    private ServiceProvider _services = null!;

    private string ConnectionString =>
        new NpgsqlConnectionStringBuilder(ServerConnectionString) { Database = _database }.ConnectionString;

    public async Task InitializeAsync()
    {
        await using (var server = new NpgsqlConnection(ServerConnectionString))
            await server.ExecuteAsync($"CREATE DATABASE {_database}");

        await EventStoreSchema.CreateAsync(ConnectionString);

        _services = addModule(new ServiceCollection()
                .AddLogging()
                .AddEventStore(ConnectionString)
                .AddReadModelDatabase(ConnectionString))
            .BuildServiceProvider();

        foreach (var service in _services.GetServices<IHostedService>())
            await service.StartAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        foreach (var service in _services.GetServices<IHostedService>())
            await service.StopAsync(CancellationToken.None);
        await _services.DisposeAsync();

        NpgsqlConnection.ClearAllPools();
        await using var server = new NpgsqlConnection(ServerConnectionString);
        await server.ExecuteAsync($"DROP DATABASE {_database} WITH (FORCE)");
    }

    public Task Given<TEvent>(TEvent @event) =>
        _services.GetRequiredService<IEventStore>().AppendAsync(typeof(TEvent).Name, @event!, Guid.Empty);

    public async Task<TResult> Query<TQuery, TResult>(TQuery query)
    {
        await WaitForCatchUpAsync();
        return await _services.GetRequiredService<IQueryHandler<TQuery, TResult>>().HandleAsync(query);
    }

    private async Task WaitForCatchUpAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        var deadline = DateTime.UtcNow + CatchUpTimeout;

        while (!await IsCaughtUpAsync(connection))
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"The {schema} projection did not catch up within {CatchUpTimeout}.");
            await Task.Delay(10);
        }
    }

    private async Task<bool> IsCaughtUpAsync(NpgsqlConnection connection) =>
        await connection.ExecuteScalarAsync<bool>($"SELECT to_regclass('{schema}.bookmark') IS NOT NULL")
        && await connection.ExecuteScalarAsync<bool>($"""
            SELECT (SELECT position FROM {schema}.bookmark)
                 = (SELECT COALESCE(MAX(position), 0) FROM events)
            """);
}
