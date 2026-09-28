using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.EventStore;

namespace CCC.Commands.Projects;

sealed class AddLanguageHandler(IStateLoader loader) : ICommandHandler<AddLanguage>
{
    public async Task<CommandResult> HandleAsync(AddLanguage command)
    {
        var teamAdded = Team.Exists(command.TeamId);
        var projectAdded = Project.InTeam(command.TeamId, command.ProjectId);
        var languageAdded = Language.InProject(command.ProjectId, command.LanguageCode);
        var validParentLanguage = Language.ValidParent(command.ProjectId, command.ParentLanguageCode);
        var isReferenceLanguage = Language.IsReference(command.ProjectId, command.LanguageCode);

        var context = await loader.LoadAsync(teamAdded, projectAdded, languageAdded, validParentLanguage, isReferenceLanguage);

        if (!teamAdded)
            return CommandResult.NotFound("Team not found.");

        if (!projectAdded)
            return CommandResult.NotFound("Project not found in this team.");

        if (languageAdded)
            return CommandResult.Conflict("This language has already been added to the project.");

        if (isReferenceLanguage)
            return CommandResult.Invalid("The reference language cannot be added as a new language.");

        if (!validParentLanguage)
            return CommandResult.Invalid("The parent language must be the project's reference language or a language already added to the project.");

        return await context.TryAppendAsync(
            new LanguageAdded(command.LanguageCode.Value, command.ParentLanguageCode.Value, command.ProjectId.Value),
            Tags.From(Tag.ProjectId(command.ProjectId), Tag.LanguageCode(command.LanguageCode)));
    }
}
