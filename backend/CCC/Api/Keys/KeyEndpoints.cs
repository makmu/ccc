using CCC.Domain.State;
using CCC.Domain.Events;
using CCC.EventStore;

namespace CCC.Api.Keys;

static class KeyEndpoints
{
    public static void MapKeyEndpoints(this WebApplication app)
    {
        app.MapPost("/teams/{teamId}/projects/{projectId}/keys", async (Guid teamId, Guid projectId, AddKeyRequest request, IStateLoader loader) =>
        {
            var teamSubscription = State.TeamSubscription(teamId);
            var projectAdded = State.ProjectInTeam(teamId, projectId);
            var keyIdTaken = State.KeyExists(teamId, projectId, request.Id);
            var keyNameTaken = State.KeyNameTaken(projectId, request.Name);
            var teamKeyCount = State.TeamKeyCount(teamId);

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
            var keyLimit = State.KeyLimit(subscriptionModelId);
            await loader.LoadAsync(keyLimit);

            if (teamKeyCount >= keyLimit)
                return Results.Conflict("The key limit for this team's subscription model has been reached.");

            try
            {
                await context.AppendAsync(new KeyAdded(request.Id, request.Name, projectId),
                    Tags.From(
                        Tag.TeamId(teamId),
                        Tag.ProjectId(projectId),
                        Tag.KeyId(request.Id),
                        Tag.KeyName(request.Name)));
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
                var teamAdded = State.TeamExists(teamId);
                var projectAdded = State.ProjectInTeam(teamId, projectId);
                var keyExists = State.KeyExists(teamId, projectId, keyId);
                var currentReferenceText = State.CurrentReferenceText(keyId);

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
                        Tags.From(Tag.KeyId(keyId)));
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
                var teamAdded = State.TeamExists(teamId);
                var projectAdded = State.ProjectInTeam(teamId, projectId);
                var keyExists = State.KeyExists(teamId, projectId, keyId);
                var languageAdded = State.LanguageInProject(projectId, request.LanguageCode);

                var currentTranslation = State.CurrentTranslation(keyId, request.LanguageCode);

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
                        Tags.From(Tag.KeyId(keyId), Tag.LanguageCode(request.LanguageCode)));
                }
                catch (ConcurrencyException)
                {
                    return Results.Conflict("A concurrent operation modified the event store. Please retry.");
                }

                return Results.Ok();
            });

        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/rename", async (Guid teamId, Guid projectId, Guid keyId, RenameKeyRequest request, IStateLoader loader) =>
        {
            var teamAdded = State.TeamExists(teamId);
            var projectAdded = State.ProjectInTeam(teamId, projectId);
            var keyExists = State.KeyExists(teamId, projectId, keyId);
            var currentKeyName = State.CurrentKeyName(keyId);
            var keyNameTaken = State.KeyNameTaken(projectId, request.Name);

            var context = await loader.LoadAsync(teamAdded, projectAdded, keyExists, currentKeyName, keyNameTaken);

            if (!teamAdded)
                return Results.NotFound("Team not found.");

            if (!projectAdded)
                return Results.NotFound("Project not found in this team.");

            if (!keyExists)
                return Results.NotFound("Key not found in this project.");

            if (keyNameTaken)
                return Results.Conflict("A key with this name already exists in this project.");

            if (currentKeyName == request.Name)
                return Results.Ok();

            try
            {
                await context.AppendAsync(new KeyRenamed(keyId, projectId, request.Name),
                    Tags.From(
                        Tag.TeamId(teamId),
                        Tag.KeyId(keyId), Tag.ProjectId(projectId), Tag.KeyName(currentKeyName.State), Tag.KeyName(request.Name)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}