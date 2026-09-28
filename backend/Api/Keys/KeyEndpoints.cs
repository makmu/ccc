using CCC.Commands;
using CCC.Commands.Keys;
using CCC.Domain.Types;

namespace CCC.Api.Keys;

static class KeyEndpoints
{
    public static void MapKeyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/teams/{teamId}/projects/{projectId}/keys",
            async (Guid teamId, Guid projectId, AddKeyRequest request, ICommandHandler<AddKey> handler) =>
            {
                var command = new AddKey(
                    TeamId.From(teamId),
                    ProjectId.From(projectId),
                    KeyId.From(request.Id),
                    KeyName.From(request.Name));
                return (await handler.HandleAsync(command)).ToHttpResult();
            });

        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/reference-text",
            async (Guid teamId, Guid projectId, Guid keyId, ProvideReferenceTextRequest request, ICommandHandler<ProvideReferenceText> handler) =>
            {
                var command = new ProvideReferenceText(
                    TeamId.From(teamId),
                    ProjectId.From(projectId),
                    KeyId.From(keyId),
                    request.Text);
                return (await handler.HandleAsync(command)).ToHttpResult();
            });

        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/translations",
            async (Guid teamId, Guid projectId, Guid keyId, ProvideTranslationRequest request, ICommandHandler<ProvideTranslation> handler) =>
            {
                var command = new ProvideTranslation(
                    TeamId.From(teamId),
                    ProjectId.From(projectId),
                    KeyId.From(keyId),
                    LanguageCode.From(request.LanguageCode),
                    request.Text);
                return (await handler.HandleAsync(command)).ToHttpResult();
            });

        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/rename",
            async (Guid teamId, Guid projectId, Guid keyId, RenameKeyRequest request, ICommandHandler<RenameKey> handler) =>
            {
                var command = new RenameKey(
                    TeamId.From(teamId),
                    ProjectId.From(projectId),
                    KeyId.From(keyId),
                    KeyName.From(request.Name));
                return (await handler.HandleAsync(command)).ToHttpResult();
            });
    }
}
