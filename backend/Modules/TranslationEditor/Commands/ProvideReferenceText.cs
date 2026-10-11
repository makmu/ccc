using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TranslationEditor.Commands;

public record ProvideReferenceText(TeamId TeamId, ProjectId ProjectId, KeyId KeyId, Text? Text);

sealed class ProvideReferenceTextHandler(IStateLoader loader) : ICommandHandler<ProvideReferenceText>
{
    public async Task<CommandResult> HandleAsync(ProvideReferenceText command, CancellationToken cancellationToken)
    {
        var teamExists = Team.Exists(command.TeamId);
        var projectInTeam = Project.InTeam(command.TeamId, command.ProjectId);
        var keyExists = Key.Exists(command.TeamId, command.ProjectId, command.KeyId);
        var alreadyProvided = Key.HasReferenceText(command.KeyId, command.Text);
        var context = await loader.LoadAsync([teamExists, projectInTeam, keyExists, alreadyProvided], cancellationToken);

        if (teamExists.IsFalse())
            return CommandResult.NotFound("Team not found.");

        if (projectInTeam.IsFalse())
            return CommandResult.NotFound("Project not found in this team.");

        if (keyExists.IsFalse())
            return CommandResult.NotFound("Key not found in this project.");

        if (alreadyProvided.IsTrue())
            return CommandResult.Ok;

        var tags = Tags.From(Tag.KeyId(command.KeyId));
        var appended = command.Text is { } text
            ? await context.TryAppendAsync(
                new ReferenceTextProvided(command.KeyId.Value, command.ProjectId.Value, text.Value), tags, cancellationToken)
            : await context.TryAppendAsync(
                new ReferenceTextWithdrawn(command.KeyId.Value, command.ProjectId.Value), tags, cancellationToken);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
