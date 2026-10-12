using Dapper;
using Npgsql;

namespace CCC.Base.TestKit;

// A throwaway database on the server given by CCC_TEST_POSTGRES, defaulting to the
// local development server.
sealed class TestDatabase
{
    private static readonly string ServerConnectionString =
        Environment.GetEnvironmentVariable("CCC_TEST_POSTGRES")
        ?? "Host=localhost:5432;Username=postgres;Password=mypass";

    private readonly string _name = $"ccc_test_{Guid.NewGuid():N}";

    public string ConnectionString =>
        new NpgsqlConnectionStringBuilder(ServerConnectionString) { Database = _name }.ConnectionString;

    public async Task CreateAsync()
    {
        await using var server = new NpgsqlConnection(ServerConnectionString);
        await server.ExecuteAsync($"CREATE DATABASE {_name}");
    }

    public async Task DropAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var server = new NpgsqlConnection(ServerConnectionString);
        await server.ExecuteAsync($"DROP DATABASE {_name} WITH (FORCE)");
    }
}
