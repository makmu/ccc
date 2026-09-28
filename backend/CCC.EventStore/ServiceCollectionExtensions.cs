using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.EventStore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventStore(this IServiceCollection services, string connectionString)
    {
        SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
        SqlMapper.AddTypeHandler(new StringArrayTypeHandler());

        services.AddSingleton<IEventStore>(new PostgresEventStore(connectionString));
        services.AddTransient<IStateLoader, StateLoader>();
        return services;
    }
}
