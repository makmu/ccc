using CCC.Domain;
using CCC.Domain.Events;
using CCC.Helpers;
using CCC.Infrastructure.EventStore;

namespace CCC.Api.Projects;

static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        app.MapPost("/teams/{teamId}/projects", async (Guid teamId, AddProjectRequest request, CommandContextBuilder contextBuilder) =>
        {
            var teamAdded = Model.TeamExists(teamId);
            var projectedIdTaken = Model.ProjectIdTaken(request.Id);

            var canonicalName = request.Name.CanonicalName();
            var projectNameTaken = Model.ProjectNameTaken(teamId, request.Name);

            var context = await contextBuilder.BuildStateModelsAsync(teamAdded, projectedIdTaken, projectNameTaken);

            if (!teamAdded)
                return Results.NotFound("Team not found.");

            if (projectedIdTaken)
                return Results.Conflict("A project with this ID already exists.");

            if (projectNameTaken)
                return Results.Conflict("A project with this name already exists in this team.");

            try
            {
                await context.AppendAsync(new ProjectAdded(request.Id, request.Name, teamId, request.ReferenceLanguage),
                    Tags.From(Tag.ProjectId(request.Id), Tag.TeamId(teamId), Tag.ProjectName(canonicalName), Tag.LanguageCode(request.ReferenceLanguage)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/teams/{teamId}/projects/{projectId}/languages", async (Guid teamId, Guid projectId, AddLanguageRequest request, CommandContextBuilder contextBuilder) =>
        {
            var teamAdded = Model.TeamExists(teamId);
            var projectAdded = Model.ProjectInTeam(teamId, projectId);
            var languageAdded = Model.LanguageInProject(projectId, request.LanguageCode);
            var validParentLanguage = Model.ValidParentLanguage(projectId, request.ParentLanguageCode);
            var isReferenceLanguage = Model.IsReferenceLanguage(projectId, request.LanguageCode);

            var context = await contextBuilder.BuildStateModelsAsync(teamAdded, projectAdded, languageAdded, validParentLanguage,  isReferenceLanguage);

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
                await context.AppendAsync(new LanguageAdded(request.LanguageCode, request.ParentLanguageCode, projectId)
                    , Tags.From(Tag.ProjectId(projectId), Tag.LanguageCode(request.LanguageCode)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}