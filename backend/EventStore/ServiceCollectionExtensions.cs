using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CCC.EventStore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventStore(this IServiceCollection services, string connectionString)
    {
        SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
        SqlMapper.AddTypeHandler(new StringArrayTypeHandler());

        services.TryAddSingleton<AppendSignal>();
        services.AddSingleton<IEventStore>(provider => new PostgresEventStore(connectionString, provider.GetRequiredService<AppendSignal>()));
        services.AddTransient<IStateLoader, StateLoader>();
        return services;
    }
}
