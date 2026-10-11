using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.Modules.AppAdministration.Commands;

namespace CCC.Api.AppAdministration.Commands;

static class AddUserEndpoint
{
    public record AddUserRequest(Guid Id, string Name, string Email);

    public static void MapAddUser(this IEndpointRouteBuilder app) =>
        app.MapPost("/users",
            async (AddUserRequest request, ICommandHandler<AddUser> handler) =>
            {
                var command = new AddUser(UserId.From(request.Id), UserName.From(request.Name), EmailAddress.From(request.Email));
                return (await handler.HandleAsync(command)).ToHttpResult();
            });
}
