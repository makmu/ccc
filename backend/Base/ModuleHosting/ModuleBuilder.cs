using Microsoft.Extensions.DependencyInjection;

namespace CCC.Base.ModuleHosting;

public sealed class ModuleBuilder(IServiceCollection services, Func<IServiceProvider, ModuleDatabase> database)
{
    public IServiceCollection Services { get; } = services;

    public T Create<T>(IServiceProvider provider) => ActivatorUtilities.CreateInstance<T>(provider, database(provider));
}
