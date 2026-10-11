using CCC.Base.Abstractions;
using CCC.Modules.AppAdministration.Queries;

namespace CCC.Api.AppAdministration.Queries;

static class UsersEndpoint
{
    public static void MapUsers(this IEndpointRouteBuilder app) =>
        app.MapGet("/users",
            async (IQueryHandler<Users, IReadOnlyList<UserSummary>> handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(new Users(), cancellationToken)));
}
