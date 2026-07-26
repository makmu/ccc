using System.Text.Json;
using CCC.Helpers;
using CCC.Infrastructure.EventStore;
using CCC.Tenants;

namespace CCC.Projects;

static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        app.MapPost("/teams/{teamId}/projects", async (Guid teamId, AddProjectRequest request, CommandContextBuilder contextBuilder) =>
        {
            var teamAdded = StateModel.Init(false)
                .Where("team-id", teamId.ToString())
                .When<TeamAdded>((_, _) => true);

            var projectedIdTaken = StateModel.Init(false)
                .Where("project-id", request.Id.ToString())
                .When<ProjectAdded>((_, _) => true);

            var canonicalName = request.Name.CanonicalName();
            var projectNameTaken = StateModel.Init(false)
                .Where("team-id", teamId.ToString())
                .Where("project-name", canonicalName)
                .When<ProjectAdded>((_, _) => true);

            var context = await contextBuilder.BuildStateModelsAsync(teamAdded, projectedIdTaken, projectNameTaken);

            if (!teamAdded.State)
                return Results.NotFound("Team not found.");

            if (projectedIdTaken.State)
                return Results.Conflict("A project with this ID already exists.");

            if (projectNameTaken.State)
                return Results.Conflict("A project with this name already exists in this team.");

            try
            {
                await context.AppendAsync(new ProjectAdded(request.Id, request.Name, teamId, request.ReferenceLanguage),
                    Tags.From(("project-id", request.Id.ToString()), ("team-id", teamId.ToString()), ("project-name", canonicalName), ("language-code", request.ReferenceLanguage)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/teams/{teamId}/projects/{projectId}/languages", async (Guid teamId, Guid projectId, AddLanguageRequest request, CommandContextBuilder contextBuilder) =>
        {
            var teamAdded = StateModel.Init(false)
                .Where("team-id", teamId.ToString())
                .When<TeamAdded>((_, _) => true);
            
            var projectAdded = StateModel.Init(false)
                .Where("team-id", teamId.ToString())
                .Where("project-id", projectId.ToString())
                .When<ProjectAdded>((_, _) => true);
            
            var languageAdded = StateModel.Init(false)
                .Where("project-id", projectId.ToString())
                .Where("language-code", request.LanguageCode)
                .When<LanguageAdded>((_, _) => true);
            
            var validParentLanguage = StateModel.Init(false)
                .Where("project-id", projectId.ToString())
                .Where("language-code", request.ParentLanguageCode)
                .When<ProjectAdded>((_, _) => true)
                .When<LanguageAdded>((e, _) => e.LanguageCode == request.ParentLanguageCode);

            var isReferenceLanguage = StateModel.Init(false)
                .Where("project-id", projectId.ToString())
                .Where("language-code", request.LanguageCode)
                .When<ProjectAdded>((_, _) => true);
            
            var context = await contextBuilder.BuildStateModelsAsync(teamAdded, projectAdded, languageAdded, validParentLanguage,  isReferenceLanguage);

            if (teamAdded.State == false)
                return Results.NotFound("Team not found.");

            if (projectAdded.State == false)
                return Results.NotFound("Project not found in this team.");

            if (languageAdded.State)
                return Results.Conflict("This language has already been added to the project.");
            
            if(isReferenceLanguage.State)
                return Results.BadRequest("The reference language cannot be added as a new language.");

            if (validParentLanguage.State == false)
                return Results.BadRequest("The parent language must be the project's reference language or a language already added to the project.");

            try
            {
                await context.AppendAsync(new LanguageAdded(request.LanguageCode, request.ParentLanguageCode, projectId)
                    , Tags.From(("project-id", projectId.ToString()), ("language-code", request.LanguageCode)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}