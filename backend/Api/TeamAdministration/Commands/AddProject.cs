using CCC.Base.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.TeamAdministration.Commands;

namespace CCC.Api.TeamAdministration.Commands;

static class AddProjectEndpoint
{
    public sealed record AddProjectRequest(Guid Id, string Name, string ReferenceLanguage);

    public static void MapAddProject(this IEndpointRouteBuilder app) =>
        app.MapPost("/teams/{teamId}/projects",
            async (Guid teamId, AddProjectRequest request, ICommandHandler<AddProject> handler, CancellationToken cancellationToken) =>
            {
                var command = new AddProject(
                    TeamId.From(teamId),
                    ProjectId.From(request.Id),
                    ProjectName.From(request.Name),
                    LanguageCode.From(request.ReferenceLanguage));
                return (await handler.HandleAsync(command, cancellationToken)).ToHttpResult();
            });
}
