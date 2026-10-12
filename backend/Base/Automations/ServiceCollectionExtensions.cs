using CCC.Base.Projections;
using CCC.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CCC.Base.Automations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAutomation<TAutomation>(this IServiceCollection services)
        where TAutomation : class, IAutomation
    {
        services.TryAddSingleton<AppendSignal>();
        services.TryAddSingleton(ProjectionRetryPolicy.Default);
        services.AddSingleton<TAutomation>();
        services.AddHostedService<AutomationRunner<TAutomation>>();
        services.AddHealthChecks();
        services.AddOptions<HealthCheckServiceOptions>().Configure<TAutomation>((options, automation) =>
            options.Registrations.Add(new HealthCheckRegistration(automation.Schema,
                provider => ActivatorUtilities.CreateInstance<AutomationHealthCheck<TAutomation>>(provider), null, null)));
        return services;
    }
}
