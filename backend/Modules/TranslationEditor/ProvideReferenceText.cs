using CCC.Abstractions;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TranslationEditor;

public record ProvideReferenceText(TeamId TeamId, ProjectId ProjectId, KeyId KeyId, Text Text);

sealed class ProvideReferenceTextHandler(IStateLoader loader) : ICommandHandler<ProvideReferenceText>
{
    public async Task<CommandResult> HandleAsync(ProvideReferenceText command)
    {
        var (teamExists, projectInTeam, keyExists, alreadyProvided, context) = await loader.LoadAsync(
            Team.Exists(command.TeamId),
            Project.InTeam(command.TeamId, command.ProjectId),
            Key.Exists(command.TeamId, command.ProjectId, command.KeyId),
            Key.HasReferenceText(command.KeyId, command.Text));

        if (!teamExists)
            return CommandResult.NotFound("Team not found.");

        if (!projectInTeam)
            return CommandResult.NotFound("Project not found in this team.");

        if (!keyExists)
            return CommandResult.NotFound("Key not found in this project.");

        if (alreadyProvided)
            return CommandResult.Ok;

        var appended = await context.TryAppendAsync(
            new ReferenceTextProvided(command.KeyId.Value, command.ProjectId.Value, command.Text.Value),
            Tags.From(Tag.KeyId(command.KeyId)));
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
