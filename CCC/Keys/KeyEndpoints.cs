using System.Text.Json;
using CCC.Infrastructure.EventStore;
using CCC.Organizations;
using CCC.Projects;
using CCC.SubscriptionModels;

namespace CCC.Keys;

static class KeyEndpoints
{
    public static void MapKeyEndpoints(this WebApplication app)
    {
        app.MapPost("/teams/{teamId}/projects/{projectId}/keys", async (Guid teamId, Guid projectId, AddKeyRequest request, CommandContextBuilder contextBuilder) =>
        {
            var context = await contextBuilder
                .Where<TeamAdded>(w => w.With(e => e.Id, teamId))
                .Where<ProjectAdded>(w => w.With(e => e.TeamId, teamId))
                .Where<SubscriptionModelAdded>()
                .Where<KeyAdded>()
                .LoadAsync();

            if (context.Events.All(e => e.Type != nameof(TeamAdded)))
                return Results.NotFound("Team not found.");

            var project = context.Events
                .Where(e => e.Type == nameof(ProjectAdded))
                .Select(e => JsonSerializer.Deserialize<ProjectAdded>(e.Payload)!)
                .SingleOrDefault(p => p.Id == projectId);

            if (project is null)
                return Results.NotFound("Project not found in this team.");

            var team = context.Events
                .Where(e => e.Type == nameof(TeamAdded))
                .Select(e => JsonSerializer.Deserialize<TeamAdded>(e.Payload)!)
                .Single();

            var subscriptionModel = context.Events
                .Where(e => e.Type == nameof(SubscriptionModelAdded))
                .Select(e => JsonSerializer.Deserialize<SubscriptionModelAdded>(e.Payload)!)
                .Single(m => m.Id == team.SubscriptionModelId);

            var teamProjectIds = context.Events
                .Where(e => e.Type == nameof(ProjectAdded))
                .Select(e => JsonSerializer.Deserialize<ProjectAdded>(e.Payload)!.Id)
                .ToHashSet();

            var keys = context.Events
                .Where(e => e.Type == nameof(KeyAdded))
                .Select(e => JsonSerializer.Deserialize<KeyAdded>(e.Payload)!)
                .Where(k => teamProjectIds.Contains(k.ProjectId))
                .ToList();

            if (keys.Count >= subscriptionModel.KeyLimit)
                return Results.Conflict("The key limit for this team's subscription model has been reached.");

            if (keys.Any(k => k.ProjectId == projectId && k.Name == request.Name))
                return Results.Conflict("A key with this name already exists in this project.");

            try
            {
                await context.AppendAsync(new KeyAdded(request.Name, projectId));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
