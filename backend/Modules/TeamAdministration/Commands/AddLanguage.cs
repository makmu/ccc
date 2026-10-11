using CCC.Abstractions;
using CCC.Dcb;
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
        var (teamExists, projectInTeam, languageAdded, validParent, isReference, context) = await loader.LoadAsync(
            Team.Exists(command.TeamId),
            Project.InTeam(command.TeamId, command.ProjectId),
            Language.InProject(command.ProjectId, command.LanguageCode),
            Language.ValidParent(command.ProjectId, command.ParentLanguageCode),
            Language.IsReference(command.ProjectId, command.LanguageCode),
            cancellationToken);

        if (!teamExists)
            return CommandResult.NotFound("Team not found.");

        if (!projectInTeam)
            return CommandResult.NotFound("Project not found in this team.");

        if (languageAdded)
            return CommandResult.Conflict("This language has already been added to the project.");

        if (isReference)
            return CommandResult.Invalid("The reference language cannot be added as a new language.");

        if (!validParent)
            return CommandResult.Invalid("The parent language must be the project's reference language or a language already added to the project.");

        var appended = await context.TryAppendAsync(
            new LanguageAdded(command.LanguageCode.Value, command.ParentLanguageCode.Value, command.ProjectId.Value),
            Tags.From(Tag.ProjectId(command.ProjectId), Tag.LanguageCode(command.LanguageCode)),
            cancellationToken);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
