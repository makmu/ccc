using System.Text.Json;
using CCC.Infrastructure.EventStore;
using CCC.Organizations;

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
    }
}
