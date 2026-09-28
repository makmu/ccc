using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.EventStore;

namespace CCC.Commands.Keys;

sealed class ProvideReferenceTextHandler(IStateLoader loader) : ICommandHandler<ProvideReferenceText>
{
    public async Task<CommandResult> HandleAsync(ProvideReferenceText command)
    {
        var teamAdded = Team.Exists(command.TeamId);
        var projectAdded = Project.InTeam(command.TeamId, command.ProjectId);
        var keyExists = Key.Exists(command.TeamId, command.ProjectId, command.KeyId);
        var referenceText = Key.ReferenceText(command.KeyId);

        var context = await loader.LoadAsync(teamAdded, projectAdded, keyExists, referenceText);

        if (!teamAdded)
            return CommandResult.NotFound("Team not found.");

        if (!projectAdded)
            return CommandResult.NotFound("Project not found in this team.");

        if (!keyExists)
            return CommandResult.NotFound("Key not found in this project.");

        if (referenceText == command.Text)
            return CommandResult.Ok;

        return await context.TryAppendAsync(
            new ReferenceTextProvided(command.KeyId.Value, command.ProjectId.Value, command.Text),
            Tags.From(Tag.KeyId(command.KeyId)));
    }
}
