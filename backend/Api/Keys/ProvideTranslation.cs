using CCC.Commands;
using CCC.Commands.Keys;
using CCC.Domain.Types;

namespace CCC.Api.Keys;

static class ProvideTranslationEndpoint
{
    public record ProvideTranslationRequest(string LanguageCode, string Text);

    public static void MapProvideTranslation(this IEndpointRouteBuilder app) =>
        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/translations",
            async (Guid teamId, Guid projectId, Guid keyId, ProvideTranslationRequest request, ICommandHandler<ProvideTranslation> handler) =>
            {
                var command = new ProvideTranslation(
                    TeamId.From(teamId),
                    ProjectId.From(projectId),
                    KeyId.From(keyId),
                    LanguageCode.From(request.LanguageCode),
                    Text.From(request.Text));
                return (await handler.HandleAsync(command)).ToHttpResult();
            });
}
