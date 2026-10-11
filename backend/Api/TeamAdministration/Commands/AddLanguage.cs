using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TeamAdministration.Commands;

namespace CCC.Api.TeamAdministration.Commands;

static class AddLanguageEndpoint
{
    public record AddLanguageRequest(string LanguageCode, string ParentLanguageCode);

    public static void MapAddLanguage(this IEndpointRouteBuilder app) =>
        app.MapPost("/teams/{teamId}/projects/{projectId}/languages",
            async (Guid teamId, Guid projectId, AddLanguageRequest request, ICommandHandler<AddLanguage> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new AddLanguage(
                    TeamId.From(teamId),
                    ProjectId.From(projectId),
                    LanguageCode.From(request.LanguageCode),
                    LanguageCode.From(request.ParentLanguageCode));
                return (await handler.HandleAsync(command, cancellationToken)).ToHttpResult();
            });
}
