using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TranslationEditor.Queries;

namespace CCC.Api.TranslationEditor.Queries;

static class KeysOfProjectEndpoint
{
    public static void MapKeysOfProject(this IEndpointRouteBuilder app) =>
        app.MapGet("/teams/{teamId}/projects/{projectId}/keys",
            async (Guid teamId, Guid projectId, IQueryHandler<KeysOfProject, ProjectKeys?> handler, CancellationToken cancellationToken) =>
            {
                var keys = await handler.HandleAsync(new KeysOfProject(TeamId.From(teamId), ProjectId.From(projectId)), cancellationToken);
                return keys is null ? Results.NotFound("Project not found in this team.") : Results.Ok(keys);
            });
}
