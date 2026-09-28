using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.ValueObjects;
using CCC.EventStore;

namespace CCC.Api.Users;

static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users", async (AddUserRequest request, IStateLoader loader) =>
        {
            var userId = UserId.Create(request.Id);
            var email = EmailAddress.Create(request.Email);

            var idTaken = State.UserExists(userId);
            var emailTaken = State.UserEmailTaken(email);

            var context = await loader.LoadAsync(idTaken, emailTaken);

            if (idTaken)
                return Results.Conflict("A user with this id already exists.");
            if (emailTaken)
                return Results.Conflict("A user with this email address already exists.");

            try
            {
                await context.AppendAsync(
                    new UserAdded(request.Id, request.Name, request.Email),
                    Tags.From(Tag.UserId(userId), Tag.UserEmail(email)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
