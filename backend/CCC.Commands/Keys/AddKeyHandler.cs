using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.ValueObjects;
using CCC.EventStore;

namespace CCC.Commands.Keys;

sealed class AddKeyHandler(IStateLoader loader) : ICommandHandler<AddKey>
{
    public async Task<CommandResult> HandleAsync(AddKey command)
    {
        var teamSubscription = State.TeamSubscription(command.TeamId);
        var projectAdded = State.ProjectInTeam(command.TeamId, command.ProjectId);
        var keyIdTaken = State.KeyExists(command.TeamId, command.ProjectId, command.Id);
        var keyNameTaken = State.KeyNameTaken(command.ProjectId, command.Name);
        var teamKeyCount = State.TeamKeyCount(command.TeamId);

        var context = await loader.LoadAsync(
            teamSubscription, projectAdded, keyIdTaken, keyNameTaken, teamKeyCount);

        if (teamSubscription.State is not { } subscriptionModelId)
            return CommandResult.NotFound("Team not found.");

        if (!projectAdded)
            return CommandResult.NotFound("Project not found in this team.");

        if (keyIdTaken)
            return CommandResult.Conflict("Key ID already taken.");

        if (keyNameTaken)
            return CommandResult.Conflict("Key name already taken.");

        // The limit sits behind a team -> subscription hop, so it needs a second
        // read once the subscription id is known. KeyLimit is immutable, so it
        // does not need to be part of the (phase-1) append condition; the
        // team-id key count already is, which is what guards the invariant.
        var keyLimit = State.KeyLimit(SubscriptionModelId.Create(subscriptionModelId));
        await loader.LoadAsync(keyLimit);

        if (teamKeyCount >= keyLimit)
            return CommandResult.Conflict("The key limit for this team's subscription model has been reached.");

        return await context.TryAppendAsync(
            new KeyAdded(command.Id.Value, command.Name.Value, command.ProjectId.Value),
            Tags.From(
                Tag.TeamId(command.TeamId),
                Tag.ProjectId(command.ProjectId),
                Tag.KeyId(command.Id),
                Tag.KeyName(command.Name)));
    }
}
