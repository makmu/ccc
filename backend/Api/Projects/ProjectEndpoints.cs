using CCC.Commands;
using CCC.Commands.Projects;
using CCC.Domain.Types;

namespace CCC.Api.Projects;

static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/teams/{teamId}/projects", async (Guid teamId, AddProjectRequest request, ICommandHandler<AddProject> handler) =>
        {
            var command = new AddProject(
                TeamId.From(teamId),
                ProjectId.From(request.Id),
                ProjectName.From(request.Name),
                LanguageCode.From(request.ReferenceLanguage));
            return (await handler.HandleAsync(command)).ToHttpResult();
        });

        app.MapPost("/teams/{teamId}/projects/{projectId}/languages", async (Guid teamId, Guid projectId, AddLanguageRequest request, ICommandHandler<AddLanguage> handler) =>
        {
            var command = new AddLanguage(
                TeamId.From(teamId),
                ProjectId.From(projectId),
                LanguageCode.From(request.LanguageCode),
                LanguageCode.From(request.ParentLanguageCode));
            return (await handler.HandleAsync(command)).ToHttpResult();
        });
    }
}
