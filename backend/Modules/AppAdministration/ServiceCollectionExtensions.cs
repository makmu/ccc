using CCC.Abstractions;
using CCC.EventStore;
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
        services.AddProjection<AppAdministrationProjection>();
        services.AddTransient<IQueryHandler<SubscriptionModels, IReadOnlyList<SubscriptionModelSummary>>, SubscriptionModelsHandler>();
        services.AddTransient<IQueryHandler<Tenants, IReadOnlyList<TenantSummary>>, TenantsHandler>();
        services.AddTransient<IQueryHandler<Users, IReadOnlyList<UserSummary>>, UsersHandler>();
        return services;
    }
}
