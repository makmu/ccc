using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.AppAdministration;

namespace CCC.Api.AppAdministration;

static class AddUserEndpoint
{
    public record AddUserRequest(Guid Id, string Name, string Email);

    public static void MapAddUser(this IEndpointRouteBuilder app) =>
        app.MapPost("/users",
            async (AddUserRequest request, ICommandHandler<AddUser> handler) =>
            {
                var command = new AddUser(UserId.From(request.Id), request.Name, EmailAddress.From(request.Email));
                return (await handler.HandleAsync(command)).ToHttpResult();
            });
}
