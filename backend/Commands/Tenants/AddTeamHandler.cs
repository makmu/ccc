using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.EventStore;

namespace CCC.Commands.Tenants;

sealed class AddTeamHandler(IStateLoader loader) : ICommandHandler<AddTeam>
{
    public async Task<CommandResult> HandleAsync(AddTeam command)
    {
        var tenantIdRegistered = Tenant.Exists(command.TenantId);
        var tenantSubscriptions = Tenant.Subscriptions(command.TenantId);
        var teamIdTaken = Team.IdTaken(command.TenantId, command.Id);
        var teamNameConflict = Team.NameTaken(command.TenantId, command.Name);

        var context = await loader.LoadAsync(tenantIdRegistered, tenantSubscriptions, teamIdTaken, teamNameConflict);

        if (!tenantIdRegistered)
            return CommandResult.NotFound("Tenant not found.");

        if (!tenantSubscriptions.State.Contains(command.SubscriptionModelId.Value))
            return CommandResult.Invalid("The specified subscription model is not available in this tenant.");

        if (teamIdTaken || teamNameConflict)
            return CommandResult.Conflict("A team with this id or name already exists in this tenant.");

        return await context.TryAppendAsync(
            new TeamAdded(command.Id.Value, command.Name.Value, command.TenantId.Value, command.SubscriptionModelId.Value),
            Tags.From(Tag.TeamId(command.Id), Tag.TenantId(command.TenantId), Tag.TeamName(command.Name)));
    }
}
