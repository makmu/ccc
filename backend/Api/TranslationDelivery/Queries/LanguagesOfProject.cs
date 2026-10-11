using CCC.Base.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TranslationDelivery.Queries;

namespace CCC.Api.TranslationDelivery.Queries;

static class LanguagesOfProjectEndpoint
{
    public static void MapLanguagesOfProject(this IEndpointRouteBuilder app) =>
        app.MapGet("/teams/{teamId}/projects/{projectId}/languages",
            async (Guid teamId, Guid projectId, IQueryHandler<LanguagesOfProject, IReadOnlyList<string>?> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new LanguagesOfProject(TeamId.From(teamId), ProjectId.From(projectId));
                var languages = await handler.HandleAsync(query, cancellationToken);
                return languages is null ? Results.NotFound("Project not found in this team.") : Results.Ok(languages);
            });
}
