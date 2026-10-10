using CCC.Abstractions;
using CCC.EventStore;
using CCC.Modules.TenantAdministration.Commands;
using CCC.Modules.TenantAdministration.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.Modules.TenantAdministration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTenantAdministration(this IServiceCollection services)
    {
        services.AddTransient<ICommandHandler<AddTeam>, AddTeamHandler>();
        services.AddTransient<ICommandHandler<RenameTenant>, RenameTenantHandler>();
        services.AddTransient<IQueryHandler<DetailsOfTenant, TenantDetails?>, DetailsOfTenantHandler>();
        services.AddProjection<TenantAdministrationProjection>();
        services.AddTransient<IQueryHandler<TeamsOfTenant, IReadOnlyList<TeamSummary>>, TeamsOfTenantHandler>();
        return services;
    }
}
