using CCC.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CCC.ReadModels;

public static class ServiceCollectionExtensions
{
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
