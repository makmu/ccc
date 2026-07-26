using System.Text.Json;
using CCC.Infrastructure.EventStore;
using CCC.Tenants;
using CCC.Projects;
using CCC.SubscriptionModels;

namespace CCC.Keys;

static class KeyEndpoints
{
    public static void MapKeyEndpoints(this WebApplication app)
    {
        app.MapPost("/teams/{teamId}/projects/{projectId}/keys", async (Guid teamId, Guid projectId, AddKeyRequest request, CommandContextBuilder contextBuilder) =>
        {
            // Team existence + its subscription model id in one model (null = not found).
            var teamSubscription = StateModel.Init<Guid?>(null)
                .Where("team-id", teamId.ToString())
                .When<TeamAdded>((e, _) => e.SubscriptionModelId);

            var projectAdded = StateModel.Init(false)
                .Where("team-id", teamId.ToString())
                .Where("project-id", projectId.ToString())
                .When<ProjectAdded>((_, _) => true);

            var keyIdTaken = StateModel.Init(false)
                .Where("team-id", teamId.ToString())
                .Where("project-id", projectId.ToString())
                .Where("key-id", request.Id.ToString())
                .When<KeyAdded>((_, _) => true);

            var keyNameTaken = StateModel.Init(false)
                .Where("project-id", projectId.ToString())
                .Where("key-name", request.Name)
                .When<KeyAdded>((_, _) => true)
                .When<KeyRenamed>((e, _) => e.Name == request.Name);

            // All keys across every project in the team count toward the limit,
            // so the count folds over the shared team-id tag (no project-id).
            var teamKeyCount = StateModel.Init(0)
                .Where("team-id", teamId.ToString())
                .When<KeyAdded>((_, count) => count + 1);

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
            var keyLimit = StateModel.Init(0)
                .Where("subscription-id", subscriptionModelId.ToString())
                .When<SubscriptionModelAdded>((e, _) => e.KeyLimit);
            await contextBuilder.BuildStateModelsAsync(keyLimit);

            if (teamKeyCount.State >= keyLimit.State)
                return Results.Conflict("The key limit for this team's subscription model has been reached.");

            try
            {
                await context.AppendAsync(new KeyAdded(request.Id, request.Name, projectId),
                    Tags.From(
                        ("team-id", teamId.ToString()),
                        ("project-id", projectId.ToString()),
                        ("key-id", request.Id.ToString()),
                        ("key-name", request.Name)));
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
                var teamAdded = StateModel.Init(false)
                    .Where("team-id", teamId.ToString())
                    .When<TeamAdded>((_, _) => true);

                var projectAdded = StateModel.Init(false)
                    .Where("team-id", teamId.ToString())
                    .Where("project-id", projectId.ToString())
                    .When<ProjectAdded>((_, _) => true);

                var keyExists = StateModel.Init(false)
                    .Where("team-id", teamId.ToString())
                    .Where("project-id", projectId.ToString())
                    .Where("key-id", keyId.ToString())
                    .When<KeyAdded>((_, _) => true);

                var currentReferenceText = StateModel.Init(string.Empty)
                    .Where("key-id", keyId.ToString())
                    .When<ReferenceTextProvided>((e, _) => e.Text);

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
                        Tags.From(("key-id", keyId.ToString())));
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
                var teamAdded = StateModel.Init(false)
                    .Where("team-id", teamId.ToString())
                    .When<TeamAdded>((_, _) => true);

                var projectAdded = StateModel.Init(false)
                    .Where("team-id", teamId.ToString())
                    .Where("project-id", projectId.ToString())
                    .When<ProjectAdded>((_, _) => true);

                var keyExists = StateModel.Init(false)
                    .Where("team-id", teamId.ToString())
                    .Where("project-id", projectId.ToString())
                    .Where("key-id", keyId.ToString())
                    .When<KeyAdded>((_, _) => true);

                var languageAdded = StateModel.Init(false)
                    .Where("project-id", projectId.ToString())
                    .Where("language-code", request.LanguageCode)
                    .When<LanguageAdded>((_, _) => true);

                // TODO: check whether drafts are enabled; exist with 409 if drafts are enabled

                var currentTranslation = StateModel.Init(string.Empty)
                    .Where("key-id", keyId.ToString())
                    .Where("language-code", request.LanguageCode)
                    .When<TranslationProvided>((e, _) => e.Text);

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
                        Tags.From(("key-id", keyId.ToString()), ("language-code", request.LanguageCode)));
                }
                catch (ConcurrencyException)
                {
                    return Results.Conflict("A concurrent operation modified the event store. Please retry.");
                }

                return Results.Ok();
            });

        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/rename", async (Guid teamId, Guid projectId, Guid keyId, RenameKeyRequest request, CommandContextBuilder contextBuilder) =>
        {
            var teamAdded = StateModel.Init(false)
                .Where("team-id", teamId.ToString())
                .When<TeamAdded>((_, _) => true);

            var projectAdded = StateModel.Init(false)
                .Where("team-id", teamId.ToString())
                .Where("project-id", projectId.ToString())
                .When<ProjectAdded>((_, _) => true);

            var keyExists = StateModel.Init(false)
                .Where("team-id", teamId.ToString())
                .Where("project-id", projectId.ToString())
                .Where("key-id", keyId.ToString())
                .When<KeyAdded>((_, _) => true);

            var currentKeyName = StateModel.Init(string.Empty)
                .Where("key-id", keyId.ToString())
                .When<KeyAdded>((e, _) => e.Name)
                .When<KeyRenamed>((e, _) => e.Name);

            var keyNameTaken = StateModel.Init(false)
                .Where("key-name", request.Name)
                .Where("project-id", projectId.ToString())
                .When<KeyAdded>((_, _) => true)
                .When<KeyRenamed>((e, _) => e.Name == request.Name);

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
                        ("team-id", teamId.ToString()),
                        ("key-id", keyId.ToString()), ("project-id", projectId.ToString()), ("key-name", currentKeyName.State), ("key-name", request.Name)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}