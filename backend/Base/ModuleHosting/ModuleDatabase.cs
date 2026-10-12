using Npgsql;

namespace CCC.Base.ModuleHosting;

public abstract class ModuleDatabase
{
    private readonly NpgsqlDataSource _dataSource;

    private protected ModuleDatabase(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public ValueTask<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken) =>
        _dataSource.OpenConnectionAsync(cancellationToken);
}

public sealed class ModuleDatabase<TModule>(ModuleDatabases databases) : ModuleDatabase(databases.DataSourceOf(TModule.Name))
    where TModule : IModule;
