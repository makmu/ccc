using CCC.Commands;
using CCC.Commands.Users;
using CCC.Domain.Types;

namespace CCC.Api.Users;

static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users", async (AddUserRequest request, ICommandHandler<AddUser> handler) =>
        {
            var command = new AddUser(UserId.From(request.Id), request.Name, EmailAddress.From(request.Email));
            return (await handler.HandleAsync(command)).ToHttpResult();
        });
    }
}
