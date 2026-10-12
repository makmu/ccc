using CCC.Base.Abstractions;
using CCC.Base.ModuleHosting;
using CCC.Base.Projections;
using CCC.Modules.TeamAdministration.Commands;
using CCC.Modules.TeamAdministration.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.Modules.TeamAdministration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTeamAdministration(this IServiceCollection services)
    {
        services.AddTransient<ICommandHandler<AddProject>, AddProjectHandler>();
        services.AddTransient<ICommandHandler<AddLanguage>, AddLanguageHandler>();
        services.AddTransient<IQueryHandler<ProjectsOfTeam, IReadOnlyList<ProjectSummary>>, ProjectsOfTeamHandler>();
        services.AddModule<TeamAdministrationModule>()
            .AddProjection<TeamAdministrationProjection>();
        return services;
    }
}
