using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TranslationEditor.Commands;

public record AddKey(TeamId TeamId, ProjectId ProjectId, KeyId Id, KeyName Name);

sealed class AddKeyHandler(IStateLoader loader) : ICommandHandler<AddKey>
{
    public async Task<CommandResult> HandleAsync(AddKey command, CancellationToken cancellationToken)
    {
        var subscription = Team.Subscription(command.TeamId);
        var projectInTeam = Project.InTeam(command.TeamId, command.ProjectId);
        var idTaken = Key.IdTaken(command.Id);
        var nameTaken = Key.NameTaken(command.ProjectId, command.Name);
        var keysAdded = Team.KeysAdded(command.TeamId);
        var context = await loader.LoadAsync([subscription, projectInTeam, idTaken, nameTaken, keysAdded], cancellationToken);

        if (subscription.IsNull(out var subscriptionModelId))
            return CommandResult.NotFound("Team not found.");

        if (projectInTeam.IsFalse())
            return CommandResult.NotFound("Project not found in this team.");

        if (idTaken.IsTrue())
            return CommandResult.Conflict("Key ID already taken.");

        if (nameTaken.IsTrue())
            return CommandResult.Conflict("Key name already taken.");

        // The limit sits behind the team's subscription, so it needs a second read.
        // It stays out of the append condition because it never changes. The team's
        // key count is in the condition, and that is what guards the limit.
        var keyLimit = Subscription.KeyLimit(subscriptionModelId);
        await loader.LoadAsync([keyLimit], cancellationToken);

        if (keysAdded.Value >= keyLimit.Value)
            return CommandResult.Conflict("The key limit for this team's subscription model has been reached.");

        var appended = await context.TryAppendAsync(
            new KeyAdded(command.Id.Value, command.Name.Value, command.ProjectId.Value),
            Tags.From(
                Tag.TeamId(command.TeamId),
                Tag.ProjectId(command.ProjectId),
                Tag.KeyId(command.Id),
                Tag.KeyName(command.Name)),
            cancellationToken);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
