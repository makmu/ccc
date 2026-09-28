using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.EventStore;

namespace CCC.Commands.Projects;

sealed class AddProjectHandler(IStateLoader loader) : ICommandHandler<AddProject>
{
    public async Task<CommandResult> HandleAsync(AddProject command)
    {
        var (teamExists, idTaken, nameTaken, context) = await loader.LoadAsync(
            Team.Exists(command.TeamId),
            Project.IdTaken(command.Id),
            Project.NameTaken(command.TeamId, command.Name));

        if (!teamExists)
            return CommandResult.NotFound("Team not found.");

        if (idTaken)
            return CommandResult.Conflict("A project with this ID already exists.");

        if (nameTaken)
            return CommandResult.Conflict("A project with this name already exists in this team.");

        return await context.TryAppendAsync(
            new ProjectAdded(command.Id.Value, command.Name.Value, command.TeamId.Value, command.ReferenceLanguage.Value),
            Tags.From(
                Tag.ProjectId(command.Id),
                Tag.TeamId(command.TeamId),
                Tag.ProjectName(command.Name),
                Tag.LanguageCode(command.ReferenceLanguage)));
    }
}
