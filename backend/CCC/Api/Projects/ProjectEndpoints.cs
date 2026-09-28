using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.ValueObjects;
using CCC.EventStore;

namespace CCC.Api.Projects;

static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/teams/{teamId}/projects", async (Guid teamId, AddProjectRequest request, IStateLoader loader) =>
        {
            var team = TeamId.Create(teamId);
            var projectId = ProjectId.Create(request.Id);
            var projectName = ProjectName.Create(request.Name);
            var referenceLanguage = LanguageCode.Create(request.ReferenceLanguage);

            var teamAdded = State.TeamExists(team);
            var projectedIdTaken = State.ProjectIdTaken(projectId);
            var projectNameTaken = State.ProjectNameTaken(team, projectName);

            var context = await loader.LoadAsync(teamAdded, projectedIdTaken, projectNameTaken);

            if (!teamAdded)
                return Results.NotFound("Team not found.");

            if (projectedIdTaken)
                return Results.Conflict("A project with this ID already exists.");

            if (projectNameTaken)
                return Results.Conflict("A project with this name already exists in this team.");

            try
            {
                await context.AppendAsync(new ProjectAdded(request.Id, request.Name, teamId, request.ReferenceLanguage),
                    Tags.From(Tag.ProjectId(projectId), Tag.TeamId(team), Tag.ProjectName(projectName), Tag.LanguageCode(referenceLanguage)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/teams/{teamId}/projects/{projectId}/languages", async (Guid teamId, Guid projectId, AddLanguageRequest request, IStateLoader loader) =>
        {
            var team = TeamId.Create(teamId);
            var project = ProjectId.Create(projectId);
            var languageCode = LanguageCode.Create(request.LanguageCode);
            var parentLanguageCode = LanguageCode.Create(request.ParentLanguageCode);

            var teamAdded = State.TeamExists(team);
            var projectAdded = State.ProjectInTeam(team, project);
            var languageAdded = State.LanguageInProject(project, languageCode);
            var validParentLanguage = State.ValidParentLanguage(project, parentLanguageCode);
            var isReferenceLanguage = State.IsReferenceLanguage(project, languageCode);

            var context = await loader.LoadAsync(teamAdded, projectAdded, languageAdded, validParentLanguage, isReferenceLanguage);

            if (!teamAdded)
                return Results.NotFound("Team not found.");

            if (!projectAdded)
                return Results.NotFound("Project not found in this team.");

            if (languageAdded)
                return Results.Conflict("This language has already been added to the project.");

            if (isReferenceLanguage)
                return Results.BadRequest("The reference language cannot be added as a new language.");

            if (!validParentLanguage)
                return Results.BadRequest("The parent language must be the project's reference language or a language already added to the project.");

            try
            {
                await context.AppendAsync(new LanguageAdded(request.LanguageCode, request.ParentLanguageCode, projectId),
                    Tags.From(Tag.ProjectId(project), Tag.LanguageCode(languageCode)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
