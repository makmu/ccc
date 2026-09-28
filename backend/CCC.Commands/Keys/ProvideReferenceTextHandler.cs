using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.EventStore;

namespace CCC.Commands.Keys;

sealed class ProvideReferenceTextHandler(IStateLoader loader) : ICommandHandler<ProvideReferenceText>
{
    public async Task<CommandResult> HandleAsync(ProvideReferenceText command)
    {
        var teamAdded = State.TeamExists(command.TeamId);
        var projectAdded = State.ProjectInTeam(command.TeamId, command.ProjectId);
        var keyExists = State.KeyExists(command.TeamId, command.ProjectId, command.KeyId);
        var currentReferenceText = State.CurrentReferenceText(command.KeyId);

        var context = await loader.LoadAsync(teamAdded, projectAdded, keyExists, currentReferenceText);

        if (!teamAdded)
            return CommandResult.NotFound("Team not found.");

        if (!projectAdded)
            return CommandResult.NotFound("Project not found in this team.");

        if (!keyExists)
            return CommandResult.NotFound("Key not found in this project.");

        if (currentReferenceText == command.Text)
            return CommandResult.Ok;

        return await context.TryAppendAsync(
            new ReferenceTextProvided(command.KeyId.Value, command.ProjectId.Value, command.Text),
            Tags.From(Tag.KeyId(command.KeyId)));
    }
}
