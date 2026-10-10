using CCC.Abstractions;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Commands.Keys;

public record AddKey(TeamId TeamId, ProjectId ProjectId, KeyId Id, KeyName Name);

sealed class AddKeyHandler(IStateLoader loader) : ICommandHandler<AddKey>
{
    public async Task<CommandResult> HandleAsync(AddKey command)
    {
        var (subscription, projectInTeam, idTaken, nameTaken, keysAdded, context) = await loader.LoadAsync(
            Team.Subscription(command.TeamId),
            Project.InTeam(command.TeamId, command.ProjectId),
            Key.Exists(command.TeamId, command.ProjectId, command.Id),
            Key.NameTaken(command.ProjectId, command.Name),
            Team.KeysAdded(command.TeamId));

        if (subscription is not { } subscriptionModelId)
            return CommandResult.NotFound("Team not found.");

        if (!projectInTeam)
            return CommandResult.NotFound("Project not found in this team.");

        if (idTaken)
            return CommandResult.Conflict("Key ID already taken.");

        if (nameTaken)
            return CommandResult.Conflict("Key name already taken.");

        // The limit sits behind the team's subscription, so it needs a second read.
        // It stays out of the append condition because it never changes. The team's
        // key count is in the condition, and that is what guards the limit.
        var (keyLimit, _) = await loader.LoadAsync(Subscription.KeyLimit(subscriptionModelId));

        if (keysAdded >= keyLimit)
            return CommandResult.Conflict("The key limit for this team's subscription model has been reached.");

        var appended = await context.TryAppendAsync(
            new KeyAdded(command.Id.Value, command.Name.Value, command.ProjectId.Value),
            Tags.From(
                Tag.TeamId(command.TeamId),
                Tag.ProjectId(command.ProjectId),
                Tag.KeyId(command.Id),
                Tag.KeyName(command.Name)));
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
