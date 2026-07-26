using CCC.Domain;
using CCC.Domain.Events;
using CCC.Infrastructure.EventStore;

namespace CCC.Api.Keys;

static class KeyEndpoints
{
    public static void MapKeyEndpoints(this WebApplication app)
    {
        app.MapPost("/teams/{teamId}/projects/{projectId}/keys", async (Guid teamId, Guid projectId, AddKeyRequest request, CommandContextBuilder contextBuilder) =>
        {
            var teamSubscription = Model.TeamSubscription(teamId);
            var projectAdded = Model.ProjectInTeam(teamId, projectId);
            var keyIdTaken = Model.KeyExists(teamId, projectId, request.Id);
            var keyNameTaken = Model.KeyNameTaken(projectId, request.Name);
            var teamKeyCount = Model.TeamKeyCount(teamId);

            var context = await contextBuilder.BuildStateModelsAsync(
                teamSubscription, projectAdded, keyIdTaken, keyNameTaken, teamKeyCount);

            if (teamSubscription.State is not { } subscriptionModelId)
                return Results.NotFound("Team not found.");

            if (projectAdded.State == false)
                return Results.NotFound("Project not found in this team.");

            if (keyIdTaken.State)
                return Results.Conflict("Key ID already taken.");

            if (keyNameTaken.State)
                return Results.Conflict("Key name already taken.");

            // The limit sits behind a team -> subscription hop, so it needs a second
            // read once the subscription id is known. KeyLimit is immutable, so it
            // does not need to be part of the (phase-1) append condition; the
            // team-id key count already is, which is what guards the invariant.
            var keyLimit = Model.KeyLimit(subscriptionModelId);
            await contextBuilder.BuildStateModelsAsync(keyLimit);

            if (teamKeyCount.State >= keyLimit.State)
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
            async (Guid teamId, Guid projectId, Guid keyId, ProvideReferenceTextRequest request, CommandContextBuilder contextBuilder) =>
            {
                var teamAdded = Model.TeamExists(teamId);
                var projectAdded = Model.ProjectInTeam(teamId, projectId);
                var keyExists = Model.KeyExists(teamId, projectId, keyId);
                var currentReferenceText = Model.CurrentReferenceText(keyId);

                var context = await contextBuilder.BuildStateModelsAsync(teamAdded, projectAdded, keyExists, currentReferenceText);

                if (!teamAdded.State)
                    return Results.NotFound("Team not found.");

                if (!projectAdded.State)
                    return Results.NotFound("Project not found in this team.");

                if (!keyExists.State)
                    return Results.NotFound("Key not found in this project.");

                if (currentReferenceText.State == request.Text)
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
            async (Guid teamId, Guid projectId, Guid keyId, ProvideTranslationRequest request, CommandContextBuilder contextBuilder) =>
            {
                var teamAdded = Model.TeamExists(teamId);
                var projectAdded = Model.ProjectInTeam(teamId, projectId);
                var keyExists = Model.KeyExists(teamId, projectId, keyId);
                var languageAdded = Model.LanguageInProject(projectId, request.LanguageCode);

                var currentTranslation = Model.CurrentTranslation(keyId, request.LanguageCode);

                var context = await contextBuilder.BuildStateModelsAsync(teamAdded, projectAdded, keyExists, languageAdded, currentTranslation);

                if (!teamAdded.State)
                    return Results.NotFound("Team not found.");

                if (!projectAdded.State)
                    return Results.NotFound("Project not found in this team.");

                if (!keyExists.State)
                    return Results.NotFound("Key not found in this project.");

                if (!languageAdded.State)
                    return Results.BadRequest("The specified language has not been added to this project.");

                if (currentTranslation.State == request.Text)
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

        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/rename", async (Guid teamId, Guid projectId, Guid keyId, RenameKeyRequest request, CommandContextBuilder contextBuilder) =>
        {
            var teamAdded = Model.TeamExists(teamId);
            var projectAdded = Model.ProjectInTeam(teamId, projectId);
            var keyExists = Model.KeyExists(teamId, projectId, keyId);
            var currentKeyName = Model.CurrentKeyName(keyId);
            var keyNameTaken = Model.KeyNameTaken(projectId, request.Name);

            var context = await contextBuilder.BuildStateModelsAsync(teamAdded, projectAdded, keyExists, currentKeyName, keyNameTaken);

            if (!teamAdded.State)
                return Results.NotFound("Team not found.");

            if (!projectAdded.State)
                return Results.NotFound("Project not found in this team.");

            if (!keyExists.State)
                return Results.NotFound("Key not found in this project.");

            if (keyNameTaken.State)
                return Results.Conflict("A key with this name already exists in this project.");

            if (currentKeyName.State == request.Name)
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