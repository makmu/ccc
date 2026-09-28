using CCC.Commands;
using CCC.Commands.Keys;
using CCC.Domain.Types;

namespace CCC.Api.Keys;

static class AddKeyEndpoint
{
    public record AddKeyRequest(Guid Id, string Name);

    public static void MapAddKey(this IEndpointRouteBuilder app) =>
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
}
