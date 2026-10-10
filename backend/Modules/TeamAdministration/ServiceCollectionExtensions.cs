using CCC.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.Modules.TeamAdministration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTeamAdministration(this IServiceCollection services)
    {
        services.AddTransient<ICommandHandler<AddProject>, AddProjectHandler>();
        services.AddTransient<ICommandHandler<AddLanguage>, AddLanguageHandler>();
        return services;
    }
}
