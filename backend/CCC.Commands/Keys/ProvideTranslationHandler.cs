using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.EventStore;

namespace CCC.Commands.Keys;

sealed class ProvideTranslationHandler(IStateLoader loader) : ICommandHandler<ProvideTranslation>
{
    public async Task<CommandResult> HandleAsync(ProvideTranslation command)
    {
        var teamAdded = Team.Exists(command.TeamId);
        var projectAdded = Project.InTeam(command.TeamId, command.ProjectId);
        var keyExists = Key.Exists(command.TeamId, command.ProjectId, command.KeyId);
        var languageAdded = Language.InProject(command.ProjectId, command.LanguageCode);
        var translation = Key.Translation(command.KeyId, command.LanguageCode);

        var context = await loader.LoadAsync(teamAdded, projectAdded, keyExists, languageAdded, translation);

        if (!teamAdded)
            return CommandResult.NotFound("Team not found.");

        if (!projectAdded)
            return CommandResult.NotFound("Project not found in this team.");

        if (!keyExists)
            return CommandResult.NotFound("Key not found in this project.");

        if (!languageAdded)
            return CommandResult.Invalid("The specified language has not been added to this project.");

        if (translation == command.Text)
            return CommandResult.Ok;

        return await context.TryAppendAsync(
            new TranslationProvided(command.KeyId.Value, command.ProjectId.Value, command.LanguageCode.Value, command.Text),
            Tags.From(Tag.KeyId(command.KeyId), Tag.LanguageCode(command.LanguageCode)));
    }
}
