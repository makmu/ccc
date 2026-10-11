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

    public static IServiceCollection AddReadModelDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(_ => new ReadModelDatabase(connectionString));
        return services;
    }

    public static IServiceCollection AddProjection<TProjection>(this IServiceCollection services)
        where TProjection : class, IProjection
    {
        services.TryAddSingleton<AppendSignal>();
        services.TryAddSingleton(ProjectionRetryPolicy.Default);
        services.AddSingleton<TProjection>();
        services.AddHostedService<ProjectionRunner<TProjection>>();
        return services;
    }
}
