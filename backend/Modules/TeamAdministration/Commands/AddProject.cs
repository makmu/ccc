using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TeamAdministration.Commands;

public record AddProject(TeamId TeamId, ProjectId Id, ProjectName Name, LanguageCode ReferenceLanguage);

sealed class AddProjectHandler(IStateLoader loader) : ICommandHandler<AddProject>
{
    public async Task<CommandResult> HandleAsync(AddProject command, CancellationToken cancellationToken)
    {
        var (teamExists, idTaken, nameTaken, context) = await loader.LoadAsync(
            Team.Exists(command.TeamId),
            Project.IdTaken(command.Id),
            Project.NameTaken(command.TeamId, command.Name),
            cancellationToken);

        if (!teamExists)
            return CommandResult.NotFound("Team not found.");

        if (idTaken)
            return CommandResult.Conflict("A project with this ID already exists.");

        if (nameTaken)
            return CommandResult.Conflict("A project with this name already exists in this team.");

        var appended = await context.TryAppendAsync(
            new ProjectAdded(command.Id.Value, command.Name.Value, command.TeamId.Value, command.ReferenceLanguage.Value),
            Tags.From(
                Tag.ProjectId(command.Id),
                Tag.TeamId(command.TeamId),
                Tag.ProjectName(command.Name),
                Tag.LanguageCode(command.ReferenceLanguage)),
                cancellationToken);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
