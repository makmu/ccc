using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TeamAdministration.Commands;

public record AddLanguage(TeamId TeamId, ProjectId ProjectId, LanguageCode LanguageCode, LanguageCode ParentLanguageCode);

sealed class AddLanguageHandler(IStateLoader loader) : ICommandHandler<AddLanguage>
{
    public async Task<CommandResult> HandleAsync(AddLanguage command, CancellationToken cancellationToken)
    {
        var teamExists = Team.Exists(command.TeamId);
        var projectInTeam = Project.InTeam(command.TeamId, command.ProjectId);
        var languageAdded = Language.InProject(command.ProjectId, command.LanguageCode);
        var validParent = Language.ValidParent(command.ProjectId, command.ParentLanguageCode);
        var isReference = Language.IsReference(command.ProjectId, command.LanguageCode);
        var context = await loader.LoadAsync([teamExists, projectInTeam, languageAdded, validParent, isReference], cancellationToken);

        if (teamExists.IsFalse())
            return CommandResult.NotFound("Team not found.");

        if (projectInTeam.IsFalse())
            return CommandResult.NotFound("Project not found in this team.");

        if (languageAdded.IsTrue())
            return CommandResult.Conflict("This language has already been added to the project.");

        if (isReference.IsTrue())
            return CommandResult.Invalid("The reference language cannot be added as a new language.");

        if (validParent.IsFalse())
            return CommandResult.Invalid("The parent language must be the project's reference language or a language already added to the project.");

        var appended = await context.TryAppendAsync(
            new LanguageAdded(command.LanguageCode.Value, command.ParentLanguageCode.Value, command.ProjectId.Value),
            Tags.From(Tag.ProjectId(command.ProjectId), Tag.LanguageCode(command.LanguageCode)),
            cancellationToken);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
