using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TenantAdministration.Commands;

public record AddDefaultTeam(TenantId TenantId, TeamId Id);

sealed class AddDefaultTeamHandler(IStateLoader loader) : ICommandHandler<AddDefaultTeam>
{
    private static readonly TeamName DefaultName = TeamName.From("Default");

    public async Task<CommandResult> HandleAsync(AddDefaultTeam command, CancellationToken cancellationToken)
    {
        var tenantExists = Tenant.Exists(command.TenantId);
        var subscriptionModels = Tenant.SubscriptionModels(command.TenantId);
        var hasTeams = Tenant.HasTeams(command.TenantId);
        var idTaken = Team.IdTaken(command.Id);
        var context = await loader.LoadAsync([tenantExists, subscriptionModels, hasTeams, idTaken], cancellationToken);

        if (tenantExists.IsFalse())
            return CommandResult.NotFound("Tenant not found.");

        if (hasTeams.IsTrue())
            return CommandResult.Ok;

        if (idTaken.IsTrue())
            return CommandResult.Conflict("A team with this id already exists.");

        var subscriptionModel = subscriptionModels.List()[0];
        var appended = await context.TryAppendAsync(
            new TeamAdded(command.Id.Value, DefaultName.Value, command.TenantId.Value, subscriptionModel.Value),
            Tags.From(Tag.TeamId(command.Id), Tag.TenantId(command.TenantId), Tag.TeamName(DefaultName)),
            cancellationToken);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
