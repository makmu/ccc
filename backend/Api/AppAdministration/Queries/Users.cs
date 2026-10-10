using CCC.Abstractions;
using CCC.Modules.AppAdministration.Queries;

namespace CCC.Api.AppAdministration.Queries;

static class UsersEndpoint
{
    public static void MapUsers(this IEndpointRouteBuilder app) =>
        app.MapGet("/users",
            async (IQueryHandler<Users, IReadOnlyList<UserSummary>> handler) =>
                Results.Ok(await handler.HandleAsync(new Users())));
}
