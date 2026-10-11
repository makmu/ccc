using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TranslationEditor.Commands;

public record RenameKey(TeamId TeamId, ProjectId ProjectId, KeyId KeyId, KeyName Name);

sealed class RenameKeyHandler(IStateLoader loader) : ICommandHandler<RenameKey>
{
    public async Task<CommandResult> HandleAsync(RenameKey command, CancellationToken cancellationToken)
    {
        var teamExists = Team.Exists(command.TeamId);
        var projectInTeam = Project.InTeam(command.TeamId, command.ProjectId);
        var keyExists = Key.Exists(command.TeamId, command.ProjectId, command.KeyId);
        var name = Key.Name(command.KeyId);
        var nameTaken = Key.NameTaken(command.ProjectId, command.Name);
        var alreadyNamed = Key.IsNamed(command.KeyId, command.Name);
        var context = await loader.LoadAsync([teamExists, projectInTeam, keyExists, name, nameTaken, alreadyNamed], cancellationToken);

        if (teamExists.IsFalse())
            return CommandResult.NotFound("Team not found.");

        if (projectInTeam.IsFalse())
            return CommandResult.NotFound("Project not found in this team.");

        if (keyExists.IsFalse() || name.IsNull(out var currentName))
            return CommandResult.NotFound("Key not found in this project.");

        if (alreadyNamed.IsTrue())
            return CommandResult.Ok;

        if (nameTaken.IsTrue())
            return CommandResult.Conflict("A key with this name already exists in this project.");

        var appended = await context.TryAppendAsync(
            new KeyRenamed(command.KeyId.Value, command.ProjectId.Value, command.Name.Value),
            Tags.From(
                Tag.TeamId(command.TeamId),
                Tag.KeyId(command.KeyId), Tag.ProjectId(command.ProjectId), Tag.KeyName(currentName), Tag.KeyName(command.Name)),
            cancellationToken);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
