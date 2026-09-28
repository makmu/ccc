using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Commands.Keys;

sealed class RenameKeyHandler(IStateLoader loader) : ICommandHandler<RenameKey>
{
    public async Task<CommandResult> HandleAsync(RenameKey command)
    {
        var teamAdded = Team.Exists(command.TeamId);
        var projectAdded = Project.InTeam(command.TeamId, command.ProjectId);
        var keyExists = Key.Exists(command.TeamId, command.ProjectId, command.KeyId);
        var keyName = Key.Name(command.KeyId);
        var keyNameTaken = Key.NameTaken(command.ProjectId, command.Name);

        var context = await loader.LoadAsync(teamAdded, projectAdded, keyExists, keyName, keyNameTaken);

        if (!teamAdded)
            return CommandResult.NotFound("Team not found.");

        if (!projectAdded)
            return CommandResult.NotFound("Project not found in this team.");

        if (!keyExists)
            return CommandResult.NotFound("Key not found in this project.");

        if (keyNameTaken)
            return CommandResult.Conflict("A key with this name already exists in this project.");

        var currentName = KeyName.Create(keyName.State);
        if (currentName == command.Name)
            return CommandResult.Ok;

        return await context.TryAppendAsync(
            new KeyRenamed(command.KeyId.Value, command.ProjectId.Value, command.Name.Value),
            Tags.From(
                Tag.TeamId(command.TeamId),
                Tag.KeyId(command.KeyId), Tag.ProjectId(command.ProjectId), Tag.KeyName(currentName), Tag.KeyName(command.Name)));
    }
}
