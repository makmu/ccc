using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TranslationEditor.Commands;

namespace CCC.Api.TranslationEditor.Commands;

static class ProvideReferenceTextEndpoint
{
    public record ProvideReferenceTextRequest(string? Text);

    public static void MapProvideReferenceText(this IEndpointRouteBuilder app) =>
        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/reference-text",
            async (Guid teamId, Guid projectId, Guid keyId, ProvideReferenceTextRequest request, ICommandHandler<ProvideReferenceText> handler) =>
            {
                var command = new ProvideReferenceText(
                    TeamId.From(teamId),
                    ProjectId.From(projectId),
                    KeyId.From(keyId),
                    request.Text is null ? null : Text.From(request.Text));
                return (await handler.HandleAsync(command)).ToHttpResult();
            });
}
