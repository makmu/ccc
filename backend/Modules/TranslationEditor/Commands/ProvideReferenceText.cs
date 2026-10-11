using CCC.Abstractions;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TranslationEditor.Commands;

public record ProvideReferenceText(TeamId TeamId, ProjectId ProjectId, KeyId KeyId, Text? Text);

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

        var tags = Tags.From(Tag.KeyId(command.KeyId));
        var appended = command.Text is { } text
            ? await context.TryAppendAsync(new ReferenceTextProvided(command.KeyId.Value, command.ProjectId.Value, text.Value), tags)
            : await context.TryAppendAsync(new ReferenceTextWithdrawn(command.KeyId.Value, command.ProjectId.Value), tags);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
