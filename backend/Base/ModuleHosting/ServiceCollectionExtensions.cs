using CCC.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CCC.Base.ModuleHosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddModuleDatabases(this IServiceCollection services, Func<string, string> connectionStringOf)
    {
        services.AddSingleton(_ => new ModuleDatabases(connectionStringOf));
        return services;
    }

    public static ModuleBuilder AddModule<TModule>(this IServiceCollection services)
        where TModule : IModule
    {
        services.TryAddSingleton<ModuleDatabase<TModule>>();
        services.TryAddSingleton<AppendSignal>();
        services.TryAddSingleton(RunnerRetryPolicy.Default);
        services.AddHealthChecks();
        return new ModuleBuilder(services, provider => provider.GetRequiredService<ModuleDatabase<TModule>>());
    }
}
