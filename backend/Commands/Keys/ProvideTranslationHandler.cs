using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.EventStore;

namespace CCC.Commands.Keys;

sealed class ProvideTranslationHandler(IStateLoader loader) : ICommandHandler<ProvideTranslation>
{
    public async Task<CommandResult> HandleAsync(ProvideTranslation command)
    {
        var (teamExists, projectInTeam, keyExists, languageAdded, alreadyProvided, context) = await loader.LoadAsync(
            Team.Exists(command.TeamId),
            Project.InTeam(command.TeamId, command.ProjectId),
            Key.Exists(command.TeamId, command.ProjectId, command.KeyId),
            Language.InProject(command.ProjectId, command.LanguageCode),
            Key.HasTranslation(command.KeyId, command.LanguageCode, command.Text));

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

        return await context.TryAppendAsync(
            new TranslationProvided(command.KeyId.Value, command.ProjectId.Value, command.LanguageCode.Value, command.Text.Value),
            Tags.From(Tag.KeyId(command.KeyId), Tag.LanguageCode(command.LanguageCode)));
    }
}
