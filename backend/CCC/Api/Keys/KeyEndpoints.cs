using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.ValueObjects;
using CCC.EventStore;

namespace CCC.Api.Keys;

static class KeyEndpoints
{
    public static void MapKeyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/teams/{teamId}/projects/{projectId}/keys", async (Guid teamId, Guid projectId, AddKeyRequest request, IStateLoader loader) =>
        {
            var team = TeamId.Create(teamId);
            var project = ProjectId.Create(projectId);
            var keyId = KeyId.Create(request.Id);
            var keyName = KeyName.Create(request.Name);

            var teamSubscription = State.TeamSubscription(team);
            var projectAdded = State.ProjectInTeam(team, project);
            var keyIdTaken = State.KeyExists(team, project, keyId);
            var keyNameTaken = State.KeyNameTaken(project, keyName);
            var teamKeyCount = State.TeamKeyCount(team);

            var context = await loader.LoadAsync(
                teamSubscription, projectAdded, keyIdTaken, keyNameTaken, teamKeyCount);

            if (teamSubscription.State is not { } subscriptionModelId)
                return Results.NotFound("Team not found.");

            if (!projectAdded)
                return Results.NotFound("Project not found in this team.");

            if (keyIdTaken)
                return Results.Conflict("Key ID already taken.");

            if (keyNameTaken)
                return Results.Conflict("Key name already taken.");

            // The limit sits behind a team -> subscription hop, so it needs a second
            // read once the subscription id is known. KeyLimit is immutable, so it
            // does not need to be part of the (phase-1) append condition; the
            // team-id key count already is, which is what guards the invariant.
            var keyLimit = State.KeyLimit(SubscriptionModelId.Create(subscriptionModelId));
            await loader.LoadAsync(keyLimit);

            if (teamKeyCount >= keyLimit)
                return Results.Conflict("The key limit for this team's subscription model has been reached.");

            try
            {
                await context.AppendAsync(new KeyAdded(request.Id, request.Name, projectId),
                    Tags.From(
                        Tag.TeamId(team),
                        Tag.ProjectId(project),
                        Tag.KeyId(keyId),
                        Tag.KeyName(keyName)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/reference-text",
            async (Guid teamId, Guid projectId, Guid keyId, ProvideReferenceTextRequest request, IStateLoader loader) =>
            {
                var team = TeamId.Create(teamId);
                var project = ProjectId.Create(projectId);
                var key = KeyId.Create(keyId);

                var teamAdded = State.TeamExists(team);
                var projectAdded = State.ProjectInTeam(team, project);
                var keyExists = State.KeyExists(team, project, key);
                var currentReferenceText = State.CurrentReferenceText(key);

                var context = await loader.LoadAsync(teamAdded, projectAdded, keyExists, currentReferenceText);

                if (!teamAdded)
                    return Results.NotFound("Team not found.");

                if (!projectAdded)
                    return Results.NotFound("Project not found in this team.");

                if (!keyExists)
                    return Results.NotFound("Key not found in this project.");

                if (currentReferenceText == request.Text)
                {
                    return Results.Ok();
                }

                try
                {
                    await context.AppendAsync(new ReferenceTextProvided(keyId, projectId, request.Text),
                        Tags.From(Tag.KeyId(key)));
                }
                catch (ConcurrencyException)
                {
                    return Results.Conflict("A concurrent operation modified the event store. Please retry.");
                }

                return Results.Ok();
            });

        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/translations",
            async (Guid teamId, Guid projectId, Guid keyId, ProvideTranslationRequest request, IStateLoader loader) =>
            {
                var team = TeamId.Create(teamId);
                var project = ProjectId.Create(projectId);
                var key = KeyId.Create(keyId);
                var languageCode = LanguageCode.Create(request.LanguageCode);

                var teamAdded = State.TeamExists(team);
                var projectAdded = State.ProjectInTeam(team, project);
                var keyExists = State.KeyExists(team, project, key);
                var languageAdded = State.LanguageInProject(project, languageCode);

                var currentTranslation = State.CurrentTranslation(key, languageCode);

                var context = await loader.LoadAsync(teamAdded, projectAdded, keyExists, languageAdded, currentTranslation);

                if (!teamAdded)
                    return Results.NotFound("Team not found.");

                if (!projectAdded)
                    return Results.NotFound("Project not found in this team.");

                if (!keyExists)
                    return Results.NotFound("Key not found in this project.");

                if (!languageAdded)
                    return Results.BadRequest("The specified language has not been added to this project.");

                if (currentTranslation == request.Text)
                    return Results.Ok();

                try
                {
                    await context.AppendAsync(new TranslationProvided(keyId, projectId, request.LanguageCode, request.Text),
                        Tags.From(Tag.KeyId(key), Tag.LanguageCode(languageCode)));
                }
                catch (ConcurrencyException)
                {
                    return Results.Conflict("A concurrent operation modified the event store. Please retry.");
                }

                return Results.Ok();
            });

        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/rename", async (Guid teamId, Guid projectId, Guid keyId, RenameKeyRequest request, IStateLoader loader) =>
        {
            var team = TeamId.Create(teamId);
            var project = ProjectId.Create(projectId);
            var key = KeyId.Create(keyId);
            var newName = KeyName.Create(request.Name);

            var teamAdded = State.TeamExists(team);
            var projectAdded = State.ProjectInTeam(team, project);
            var keyExists = State.KeyExists(team, project, key);
            var currentKeyName = State.CurrentKeyName(key);
            var keyNameTaken = State.KeyNameTaken(project, newName);

            var context = await loader.LoadAsync(teamAdded, projectAdded, keyExists, currentKeyName, keyNameTaken);

            if (!teamAdded)
                return Results.NotFound("Team not found.");

            if (!projectAdded)
                return Results.NotFound("Project not found in this team.");

            if (!keyExists)
                return Results.NotFound("Key not found in this project.");

            if (keyNameTaken)
                return Results.Conflict("A key with this name already exists in this project.");

            var currentName = KeyName.Create(currentKeyName.State);
            if (currentName == newName)
                return Results.Ok();

            try
            {
                await context.AppendAsync(new KeyRenamed(keyId, projectId, request.Name),
                    Tags.From(
                        Tag.TeamId(team),
                        Tag.KeyId(key), Tag.ProjectId(project), Tag.KeyName(currentName), Tag.KeyName(newName)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}