using CCC.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CCC.Base.Projections;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectionDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(_ => new ProjectionDatabase(connectionString));
        return services;
    }

    public static IServiceCollection AddProjection<TProjection>(this IServiceCollection services)
        where TProjection : class, IProjection
    {
        services.TryAddSingleton<AppendSignal>();
        services.TryAddSingleton(ProjectionRetryPolicy.Default);
        services.AddSingleton<TProjection>();
        services.AddHostedService<ProjectionRunner<TProjection>>();
        services.AddHealthChecks();
        services.AddOptions<HealthCheckServiceOptions>().Configure<TProjection>((options, projection) =>
            options.Registrations.Add(new HealthCheckRegistration(projection.Schema,
                provider => ActivatorUtilities.CreateInstance<ProjectionHealthCheck<TProjection>>(provider), null, null)));
        return services;
    }
}
