using CCC.Abstractions;
using CCC.Modules.AppAdministration;

namespace CCC.Api.AppAdministration;

static class UsersEndpoint
{
    public static void MapUsers(this IEndpointRouteBuilder app) =>
        app.MapGet("/users",
            async (IQueryHandler<Users, IReadOnlyList<UserSummary>> handler) =>
                Results.Ok(await handler.HandleAsync(new Users())));
}
