using CCC.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.Modules.AppAdministration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppAdministration(this IServiceCollection services)
    {
        services.AddTransient<ICommandHandler<AddSubscriptionModel>, AddSubscriptionModelHandler>();
        services.AddTransient<ICommandHandler<RegisterTenant>, RegisterTenantHandler>();
        services.AddTransient<ICommandHandler<AssignTenantOwner>, AssignTenantOwnerHandler>();
        services.AddTransient<ICommandHandler<AddUser>, AddUserHandler>();
        return services;
    }
}
