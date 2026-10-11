using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TenantAdministration.Commands;

public record AddTeam(TenantId TenantId, TeamId Id, TeamName Name, SubscriptionModelId SubscriptionModelId);

sealed class AddTeamHandler(IStateLoader loader) : ICommandHandler<AddTeam>
{
    public async Task<CommandResult> HandleAsync(AddTeam command, CancellationToken cancellationToken)
    {
        var tenantExists = Tenant.Exists(command.TenantId);
        var hasSubscription = Tenant.HasSubscription(command.TenantId, command.SubscriptionModelId);
        var idTaken = Team.IdTaken(command.Id);
        var nameTaken = Team.NameTaken(command.TenantId, command.Name);
        var context = await loader.LoadAsync([tenantExists, hasSubscription, idTaken, nameTaken], cancellationToken);

        if (tenantExists.IsFalse())
            return CommandResult.NotFound("Tenant not found.");

        if (hasSubscription.IsFalse())
            return CommandResult.Invalid("The specified subscription model is not available in this tenant.");

        if (idTaken.IsTrue() || nameTaken.IsTrue())
            return CommandResult.Conflict("A team with this id or name already exists in this tenant.");

        var appended = await context.TryAppendAsync(
            new TeamAdded(command.Id.Value, command.Name.Value, command.TenantId.Value, command.SubscriptionModelId.Value),
            Tags.From(Tag.TeamId(command.Id), Tag.TenantId(command.TenantId), Tag.TeamName(command.Name)),
            cancellationToken);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
