using CCC.Abstractions;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TranslationEditor.Commands;

public record RenameKey(TeamId TeamId, ProjectId ProjectId, KeyId KeyId, KeyName Name);

sealed class RenameKeyHandler(IStateLoader loader) : ICommandHandler<RenameKey>
{
    public async Task<CommandResult> HandleAsync(RenameKey command)
    {
        var (teamExists, projectInTeam, keyExists, name, nameTaken, alreadyNamed, context) = await loader.LoadAsync(
            Team.Exists(command.TeamId),
            Project.InTeam(command.TeamId, command.ProjectId),
            Key.Exists(command.TeamId, command.ProjectId, command.KeyId),
            Key.Name(command.KeyId),
            Key.NameTaken(command.ProjectId, command.Name),
            Key.IsNamed(command.KeyId, command.Name));

        if (!teamExists)
            return CommandResult.NotFound("Team not found.");

        if (!projectInTeam)
            return CommandResult.NotFound("Project not found in this team.");

        if (!keyExists || name is not { } currentName)
            return CommandResult.NotFound("Key not found in this project.");

        if (alreadyNamed)
            return CommandResult.Ok;

        if (nameTaken)
            return CommandResult.Conflict("A key with this name already exists in this project.");

        var appended = await context.TryAppendAsync(
            new KeyRenamed(command.KeyId.Value, command.ProjectId.Value, command.Name.Value),
            Tags.From(
                Tag.TeamId(command.TeamId),
                Tag.KeyId(command.KeyId), Tag.ProjectId(command.ProjectId), Tag.KeyName(currentName), Tag.KeyName(command.Name)));
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
