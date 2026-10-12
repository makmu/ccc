using CCC.Base.ModuleHosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CCC.Base.Automations;

public static class ServiceCollectionExtensions
{
    public static ModuleBuilder AddAutomation<TAutomation>(this ModuleBuilder module)
        where TAutomation : class, IAutomation
    {
        module.Services.AddSingleton<TAutomation>();
        module.Services.AddHostedService(module.Create<AutomationRunner<TAutomation>>);
        module.Services.AddOptions<HealthCheckServiceOptions>().Configure<TAutomation>((options, automation) =>
            options.Registrations.Add(new HealthCheckRegistration(automation.Schema, module.Create<AutomationHealthCheck<TAutomation>>, null, null)));
        return module;
    }
}
