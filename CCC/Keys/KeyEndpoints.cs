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
                .Where<KeyRenamed>(w => w.With(e => e.ProjectId, projectId))
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

            if (keys.Any(k => k.Id == request.Id))
                return Results.Conflict("A key with this ID already exists.");

            var keyRenames = context.Events
                .Where(e => e.Type == nameof(KeyRenamed))
                .Select(e => JsonSerializer.Deserialize<KeyRenamed>(e.Payload)!);

            if (CurrentKeyNames(keys.Where(k => k.ProjectId == projectId), keyRenames).Contains(request.Name))
                return Results.Conflict("A key with this name already exists in this project.");

            try
            {
                await context.AppendAsync(new KeyAdded(request.Id, request.Name, projectId));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/reference-text", async (Guid teamId, Guid projectId, Guid keyId, ProvideReferenceTextRequest request, CommandContextBuilder contextBuilder) =>
        {
            var context = await contextBuilder
                .Where<TeamAdded>(w => w.With(e => e.Id, teamId))
                .Where<ProjectAdded>(w => w.With(e => e.TeamId, teamId))
                .Where<KeyAdded>(w => w.With(e => e.ProjectId, projectId))
                .LoadAsync();

            if (context.Events.All(e => e.Type != nameof(TeamAdded)))
                return Results.NotFound("Team not found.");

            var projectExists = context.Events
                .Where(e => e.Type == nameof(ProjectAdded))
                .Select(e => JsonSerializer.Deserialize<ProjectAdded>(e.Payload)!)
                .Any(p => p.Id == projectId);

            if (!projectExists)
                return Results.NotFound("Project not found in this team.");

            var keyExists = context.Events
                .Where(e => e.Type == nameof(KeyAdded))
                .Select(e => JsonSerializer.Deserialize<KeyAdded>(e.Payload)!)
                .Any(k => k.Id == keyId);

            if (!keyExists)
                return Results.NotFound("Key not found in this project.");

            try
            {
                await context.AppendAsync(new ReferenceTextProvided(keyId, projectId, request.Text));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/translations", async (Guid teamId, Guid projectId, Guid keyId, ProvideTranslationRequest request, CommandContextBuilder contextBuilder) =>
        {
            var context = await contextBuilder
                .Where<TeamAdded>(w => w.With(e => e.Id, teamId))
                .Where<ProjectAdded>(w => w.With(e => e.TeamId, teamId))
                .Where<KeyAdded>(w => w.With(e => e.ProjectId, projectId))
                .Where<LanguageAdded>(w => w.With(e => e.ProjectId, projectId))
                .LoadAsync();

            if (context.Events.All(e => e.Type != nameof(TeamAdded)))
                return Results.NotFound("Team not found.");

            var projectExists = context.Events
                .Where(e => e.Type == nameof(ProjectAdded))
                .Select(e => JsonSerializer.Deserialize<ProjectAdded>(e.Payload)!)
                .Any(p => p.Id == projectId);

            if (!projectExists)
                return Results.NotFound("Project not found in this team.");

            var keyExists = context.Events
                .Where(e => e.Type == nameof(KeyAdded))
                .Select(e => JsonSerializer.Deserialize<KeyAdded>(e.Payload)!)
                .Any(k => k.Id == keyId);

            if (!keyExists)
                return Results.NotFound("Key not found in this project.");

            var languageExists = context.Events
                .Where(e => e.Type == nameof(LanguageAdded))
                .Select(e => JsonSerializer.Deserialize<LanguageAdded>(e.Payload)!)
                .Any(l => l.LanguageCode == request.LanguageCode);

            if (!languageExists)
                return Results.BadRequest("The specified language has not been added to this project.");

            try
            {
                await context.AppendAsync(new TranslationProvided(keyId, projectId, request.LanguageCode, request.Text));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/rename", async (Guid teamId, Guid projectId, Guid keyId, RenameKeyRequest request, CommandContextBuilder contextBuilder) =>
        {
            var context = await contextBuilder
                .Where<TeamAdded>(w => w.With(e => e.Id, teamId))
                .Where<ProjectAdded>(w => w.With(e => e.TeamId, teamId))
                .Where<KeyAdded>(w => w.With(e => e.ProjectId, projectId))
                .Where<KeyRenamed>(w => w.With(e => e.ProjectId, projectId))
                .LoadAsync();

            if (context.Events.All(e => e.Type != nameof(TeamAdded)))
                return Results.NotFound("Team not found.");

            var projectExists = context.Events
                .Where(e => e.Type == nameof(ProjectAdded))
                .Select(e => JsonSerializer.Deserialize<ProjectAdded>(e.Payload)!)
                .Any(p => p.Id == projectId);

            if (!projectExists)
                return Results.NotFound("Project not found in this team.");

            var keyExists = context.Events
                .Where(e => e.Type == nameof(KeyAdded))
                .Select(e => JsonSerializer.Deserialize<KeyAdded>(e.Payload)!)
                .Any(k => k.Id == keyId);

            if (!keyExists)
                return Results.NotFound("Key not found in this project.");

            var projectKeys = context.Events
                .Where(e => e.Type == nameof(KeyAdded))
                .Select(e => JsonSerializer.Deserialize<KeyAdded>(e.Payload)!);

            var keyRenames = context.Events
                .Where(e => e.Type == nameof(KeyRenamed))
                .Select(e => JsonSerializer.Deserialize<KeyRenamed>(e.Payload)!);

            if (CurrentKeyNames(projectKeys, keyRenames).Contains(request.Name))
                return Results.Conflict("A key with this name already exists in this project.");

            try
            {
                await context.AppendAsync(new KeyRenamed(keyId, projectId, request.Name));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }

    private static HashSet<string> CurrentKeyNames(IEnumerable<KeyAdded> keys, IEnumerable<KeyRenamed> renames)
    {
        var latestRenames = renames
            .GroupBy(r => r.KeyId)
            .ToDictionary(g => g.Key, g => g.Last().Name);

        return keys
            .Select(k => latestRenames.TryGetValue(k.Id, out var renamed) ? renamed : k.Name)
            .ToHashSet();
    }
}
