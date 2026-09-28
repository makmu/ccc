using CCC.Commands;
using CCC.Commands.Projects;
using CCC.Domain.ValueObjects;

namespace CCC.Api.Projects;

static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/teams/{teamId}/projects", async (Guid teamId, AddProjectRequest request, ICommandHandler<AddProject> handler) =>
        {
            var command = new AddProject(
                TeamId.Create(teamId),
                ProjectId.Create(request.Id),
                ProjectName.Create(request.Name),
                LanguageCode.Create(request.ReferenceLanguage));
            return (await handler.HandleAsync(command)).ToHttpResult();
        });

        app.MapPost("/teams/{teamId}/projects/{projectId}/languages", async (Guid teamId, Guid projectId, AddLanguageRequest request, ICommandHandler<AddLanguage> handler) =>
        {
            var command = new AddLanguage(
                TeamId.Create(teamId),
                ProjectId.Create(projectId),
                LanguageCode.Create(request.LanguageCode),
                LanguageCode.Create(request.ParentLanguageCode));
            return (await handler.HandleAsync(command)).ToHttpResult();
        });
    }
}
