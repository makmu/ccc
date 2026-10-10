using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TeamAdministration;

namespace CCC.Api.TeamAdministration;

static class AddProjectEndpoint
{
    public record AddProjectRequest(Guid Id, string Name, string ReferenceLanguage);

    public static void MapAddProject(this IEndpointRouteBuilder app) =>
        app.MapPost("/teams/{teamId}/projects",
            async (Guid teamId, AddProjectRequest request, ICommandHandler<AddProject> handler) =>
            {
                var command = new AddProject(
                    TeamId.From(teamId),
                    ProjectId.From(request.Id),
                    ProjectName.From(request.Name),
                    LanguageCode.From(request.ReferenceLanguage));
                return (await handler.HandleAsync(command)).ToHttpResult();
            });
}
