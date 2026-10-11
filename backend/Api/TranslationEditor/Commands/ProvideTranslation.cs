using CCC.Base.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TranslationEditor.Commands;

namespace CCC.Api.TranslationEditor.Commands;

static class ProvideTranslationEndpoint
{
    public sealed record ProvideTranslationRequest(string LanguageCode, string? Text);

    public static void MapProvideTranslation(this IEndpointRouteBuilder app) =>
        app.MapPost("/teams/{teamId}/projects/{projectId}/keys/{keyId}/translations",
            async (Guid teamId, Guid projectId, Guid keyId, ProvideTranslationRequest request, ICommandHandler<ProvideTranslation> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ProvideTranslation(
                    TeamId.From(teamId),
                    ProjectId.From(projectId),
                    KeyId.From(keyId),
                    LanguageCode.From(request.LanguageCode),
                    request.Text is null ? null : Text.From(request.Text));
                return (await handler.HandleAsync(command, cancellationToken)).ToHttpResult();
            });
}
