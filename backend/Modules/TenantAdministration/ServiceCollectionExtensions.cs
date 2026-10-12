using CCC.Base.Abstractions;
using CCC.Base.Automations;
using CCC.Base.ModuleHosting;
using CCC.Base.Projections;
using CCC.Modules.TenantAdministration.Commands;
using CCC.Modules.TenantAdministration.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.Modules.TenantAdministration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTenantAdministration(this IServiceCollection services)
    {
        services.AddTransient<ICommandHandler<AddTeam>, AddTeamHandler>();
        services.AddTransient<ICommandHandler<AddDefaultTeam>, AddDefaultTeamHandler>();
        services.AddTransient<ICommandHandler<RenameTenant>, RenameTenantHandler>();
        services.AddTransient<IQueryHandler<DetailsOfTenant, TenantDetails?>, DetailsOfTenantHandler>();
        services.AddTransient<IQueryHandler<TeamsOfTenant, IReadOnlyList<TeamSummary>>, TeamsOfTenantHandler>();
        services.AddModule<TenantAdministrationModule>()
            .AddProjection<TenantAdministrationProjection>()
            .AddAutomation<DefaultTeamAutomation>();
        return services;
    }
}
