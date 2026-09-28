using CCC.Commands.Keys;
using CCC.Commands.Projects;
using CCC.Commands.SubscriptionModels;
using CCC.Commands.Tenants;
using CCC.Commands.Users;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.Commands;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services)
    {
        services.AddTransient<ICommandHandler<AddSubscriptionModel>, AddSubscriptionModelHandler>();
        services.AddTransient<ICommandHandler<AddUser>, AddUserHandler>();
        services.AddTransient<ICommandHandler<RegisterTenant>, RegisterTenantHandler>();
        services.AddTransient<ICommandHandler<RenameTenant>, RenameTenantHandler>();
        services.AddTransient<ICommandHandler<AddTeam>, AddTeamHandler>();
        services.AddTransient<ICommandHandler<AssignTenantOwner>, AssignTenantOwnerHandler>();
        services.AddTransient<ICommandHandler<AddProject>, AddProjectHandler>();
        services.AddTransient<ICommandHandler<AddLanguage>, AddLanguageHandler>();
        services.AddTransient<ICommandHandler<AddKey>, AddKeyHandler>();
        services.AddTransient<ICommandHandler<RenameKey>, RenameKeyHandler>();
        services.AddTransient<ICommandHandler<ProvideReferenceText>, ProvideReferenceTextHandler>();
        services.AddTransient<ICommandHandler<ProvideTranslation>, ProvideTranslationHandler>();
        return services;
    }
}
