using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TranslationDelivery;

namespace CCC.Api.TranslationDelivery;

static class LanguagesOfProjectEndpoint
{
    public static void MapLanguagesOfProject(this IEndpointRouteBuilder app) =>
        app.MapGet("/teams/{teamId}/projects/{projectId}/languages",
            async (Guid teamId, Guid projectId, IQueryHandler<LanguagesOfProject, IReadOnlyList<string>?> handler) =>
            {
                var languages = await handler.HandleAsync(new LanguagesOfProject(TeamId.From(teamId), ProjectId.From(projectId)));
                return languages is null ? Results.NotFound("Project not found in this team.") : Results.Ok(languages);
            });
}
