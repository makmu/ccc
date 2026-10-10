using CCC.Abstractions;
using CCC.EventStore;
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
        services.AddProjection<TeamAdministrationProjection>();
        services.AddTransient<IQueryHandler<ProjectsOfTeam, IReadOnlyList<ProjectSummary>>, ProjectsOfTeamHandler>();
        return services;
    }
}
