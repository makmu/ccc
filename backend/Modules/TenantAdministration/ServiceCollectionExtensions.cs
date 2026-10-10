using CCC.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.Modules.TenantAdministration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTenantAdministration(this IServiceCollection services)
    {
        services.AddTransient<ICommandHandler<AddTeam>, AddTeamHandler>();
        services.AddTransient<ICommandHandler<RenameTenant>, RenameTenantHandler>();
        return services;
    }
}
