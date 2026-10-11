using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CCC.EventStore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventStore(this IServiceCollection services, string connectionString)
    {
        services.TryAddSingleton<AppendSignal>();
        services.AddSingleton(_ => new EventStoreDatabase(connectionString));
        services.AddSingleton<IEventStore, PostgresEventStore>();
        services.AddTransient<IStateLoader, StateLoader>();
        return services;
    }
}
