using System.Text.Json;
using CCC.Infrastructure.EventStore;
using CCC.Tenants;

namespace CCC.Projects;

static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        app.MapPost("/teams/{teamId}/projects", async (Guid teamId, AddProjectRequest request, CommandContextBuilder contextBuilder) =>
        {
            var context = await contextBuilder
                .Where<TeamAdded>(w => w.With(e => e.Id, teamId))
                .Where<ProjectAdded>(w => w.With(e => e.TeamId, teamId))
                .LoadAsync();

            if (context.Events.All(e => e.Type != nameof(TeamAdded)))
                return Results.NotFound("Team not found.");

            var conflict = context.Events
                .Where(e => e.Type == nameof(ProjectAdded))
                .Select(e => JsonSerializer.Deserialize<ProjectAdded>(e.Payload)!)
                .Any(p => p.Id == request.Id || p.Name == request.Name);

            if (conflict)
                return Results.Conflict("A project with this id or name already exists in this team.");

            try
            {
                await context.AppendAsync(new ProjectAdded(request.Id, request.Name, teamId, request.ReferenceLanguage));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/teams/{teamId}/projects/{projectId}/languages", async (Guid teamId, Guid projectId, AddLanguageRequest request, CommandContextBuilder contextBuilder) =>
        {
            var context = await contextBuilder
                .Where<TeamAdded>(w => w.With(e => e.Id, teamId))
                .Where<ProjectAdded>(w => w.With(e => e.TeamId, teamId))
                .Where<LanguageAdded>(w => w.With(e => e.ProjectId, projectId))
                .LoadAsync();

            if (context.Events.All(e => e.Type != nameof(TeamAdded)))
                return Results.NotFound("Team not found.");

            var project = context.Events
                .Where(e => e.Type == nameof(ProjectAdded))
                .Select(e => JsonSerializer.Deserialize<ProjectAdded>(e.Payload)!)
                .SingleOrDefault(p => p.Id == projectId);

            if (project is null)
                return Results.NotFound("Project not found in this team.");

            var existingLanguages = context.Events
                .Where(e => e.Type == nameof(LanguageAdded))
                .Select(e => JsonSerializer.Deserialize<LanguageAdded>(e.Payload)!.LanguageCode)
                .ToHashSet();

            if (existingLanguages.Contains(request.LanguageCode))
                return Results.Conflict("This language has already been added to the project.");

            var validParents = existingLanguages.Append(project.ReferenceLanguage).ToHashSet();

            if (!validParents.Contains(request.ParentLanguageCode))
                return Results.BadRequest("The parent language must be the project's reference language or a language already added to the project.");

            try
            {
                await context.AppendAsync(new LanguageAdded(request.LanguageCode, request.ParentLanguageCode, projectId));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
