using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TranslationEditor;

namespace CCC.Api.TranslationEditor;

static class KeysOfProjectEndpoint
{
    public static void MapKeysOfProject(this IEndpointRouteBuilder app) =>
        app.MapGet("/teams/{teamId}/projects/{projectId}/keys",
            async (Guid teamId, Guid projectId, IQueryHandler<KeysOfProject, ProjectKeys?> handler) =>
            {
                var keys = await handler.HandleAsync(new KeysOfProject(TeamId.From(teamId), ProjectId.From(projectId)));
                return keys is null ? Results.NotFound("Project not found in this team.") : Results.Ok(keys);
            });
}
