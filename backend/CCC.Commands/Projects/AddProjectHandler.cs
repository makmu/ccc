using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.EventStore;

namespace CCC.Commands.Projects;

sealed class AddProjectHandler(IStateLoader loader) : ICommandHandler<AddProject>
{
    public async Task<CommandResult> HandleAsync(AddProject command)
    {
        var teamAdded = Team.Exists(command.TeamId);
        var projectIdTaken = Project.IdTaken(command.Id);
        var projectNameTaken = Project.NameTaken(command.TeamId, command.Name);

        var context = await loader.LoadAsync(teamAdded, projectIdTaken, projectNameTaken);

        if (!teamAdded)
            return CommandResult.NotFound("Team not found.");

        if (projectIdTaken)
            return CommandResult.Conflict("A project with this ID already exists.");

        if (projectNameTaken)
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
