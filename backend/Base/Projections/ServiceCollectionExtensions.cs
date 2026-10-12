using CCC.Base.ModuleHosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CCC.Base.Projections;

public static class ServiceCollectionExtensions
{
    public static ModuleBuilder AddProjection<TProjection>(this ModuleBuilder module)
        where TProjection : class, IProjection
    {
        module.Services.AddSingleton<TProjection>();
        module.Services.AddHostedService(module.Create<ProjectionRunner<TProjection>>);
        module.Services.AddOptions<HealthCheckServiceOptions>().Configure<TProjection>((options, projection) =>
            options.Registrations.Add(new HealthCheckRegistration(projection.Schema, module.Create<ProjectionHealthCheck<TProjection>>, null, null)));
        return module;
    }
}
