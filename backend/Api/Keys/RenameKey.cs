using CCC.Commands;
using CCC.Commands.Keys;
using CCC.Domain.Types;

namespace CCC.Api.Keys;

static class RenameKeyEndpoint
{
    public record RenameKeyRequest(string Name);

    public static void MapRenameKey(this IEndpointRouteBuilder app) =>
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
