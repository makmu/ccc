using CCC.Domain.State;
using CCC.Domain.Events;
using CCC.EventStore;

namespace CCC.Api.Users;

static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapPost("/users", async (AddUserRequest request, IStateLoader loader) =>
        {
            var idTaken = State.UserExists(request.Id);
            var emailTaken = State.UserEmailTaken(request.Email);

            var context = await loader.LoadAsync(idTaken, emailTaken);

            if (idTaken)
                return Results.Conflict("A user with this id already exists.");
            if (emailTaken)
                return Results.Conflict("A user with this email address already exists.");

            try
            {
                await context.AppendAsync(
                    new UserAdded(request.Id, request.Name, request.Email),
                    Tags.From(Tag.UserId(request.Id), Tag.UserEmail(request.Email)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
