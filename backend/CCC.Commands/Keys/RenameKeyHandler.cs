using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.ValueObjects;
using CCC.EventStore;

namespace CCC.Commands.Keys;

sealed class RenameKeyHandler(IStateLoader loader) : ICommandHandler<RenameKey>
{
    public async Task<CommandResult> HandleAsync(RenameKey command)
    {
        var teamAdded = State.TeamExists(command.TeamId);
        var projectAdded = State.ProjectInTeam(command.TeamId, command.ProjectId);
        var keyExists = State.KeyExists(command.TeamId, command.ProjectId, command.KeyId);
        var currentKeyName = State.CurrentKeyName(command.KeyId);
        var keyNameTaken = State.KeyNameTaken(command.ProjectId, command.Name);

        var context = await loader.LoadAsync(teamAdded, projectAdded, keyExists, currentKeyName, keyNameTaken);

        if (!teamAdded)
            return CommandResult.NotFound("Team not found.");

        if (!projectAdded)
            return CommandResult.NotFound("Project not found in this team.");

        if (!keyExists)
            return CommandResult.NotFound("Key not found in this project.");

        if (keyNameTaken)
            return CommandResult.Conflict("A key with this name already exists in this project.");

        var currentName = KeyName.Create(currentKeyName.State);
        if (currentName == command.Name)
            return CommandResult.Ok;

        return await context.TryAppendAsync(
            new KeyRenamed(command.KeyId.Value, command.ProjectId.Value, command.Name.Value),
            Tags.From(
                Tag.TeamId(command.TeamId),
                Tag.KeyId(command.KeyId), Tag.ProjectId(command.ProjectId), Tag.KeyName(currentName), Tag.KeyName(command.Name)));
    }
}
