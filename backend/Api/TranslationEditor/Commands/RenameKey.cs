using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TranslationEditor.Commands;

namespace CCC.Api.TranslationEditor.Commands;

static class RenameKeyEndpoint
{
    public record RenameKeyRequest(string Name);

    public static void MapRenameKey(this IEndpointRouteBuilder app) =>
        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/rename",
            async (Guid teamId, Guid projectId, Guid keyId, RenameKeyRequest request, ICommandHandler<RenameKey> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RenameKey(
                    TeamId.From(teamId),
                    ProjectId.From(projectId),
                    KeyId.From(keyId),
                    KeyName.From(request.Name));
                return (await handler.HandleAsync(command, cancellationToken)).ToHttpResult();
            });
}
