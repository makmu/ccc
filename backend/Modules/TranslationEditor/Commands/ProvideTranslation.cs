using CCC.Abstractions;
using CCC.Dcb;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TranslationEditor.Commands;

public record ProvideTranslation(TeamId TeamId, ProjectId ProjectId, KeyId KeyId, LanguageCode LanguageCode, Text? Text);

sealed class ProvideTranslationHandler(IStateLoader loader) : ICommandHandler<ProvideTranslation>
{
    public async Task<CommandResult> HandleAsync(ProvideTranslation command, CancellationToken cancellationToken)
    {
        var (teamExists, projectInTeam, keyExists, languageAdded, alreadyProvided, context) = await loader.LoadAsync(
            Team.Exists(command.TeamId),
            Project.InTeam(command.TeamId, command.ProjectId),
            Key.Exists(command.TeamId, command.ProjectId, command.KeyId),
            Language.InProject(command.ProjectId, command.LanguageCode),
            Key.HasTranslation(command.KeyId, command.LanguageCode, command.Text),
            cancellationToken);

        if (!teamExists)
            return CommandResult.NotFound("Team not found.");

        if (!projectInTeam)
            return CommandResult.NotFound("Project not found in this team.");

        if (!keyExists)
            return CommandResult.NotFound("Key not found in this project.");

        if (!languageAdded)
            return CommandResult.Invalid("The specified language has not been added to this project.");

        if (alreadyProvided)
            return CommandResult.Ok;

        var tags = Tags.From(Tag.KeyId(command.KeyId), Tag.LanguageCode(command.LanguageCode));
        var appended = command.Text is { } text
            ? await context.TryAppendAsync(
                new TranslationProvided(command.KeyId.Value, command.ProjectId.Value, command.LanguageCode.Value, text.Value),
                tags,
                cancellationToken)
            : await context.TryAppendAsync(
                new TranslationWithdrawn(command.KeyId.Value, command.ProjectId.Value, command.LanguageCode.Value),
                tags,
                cancellationToken);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
