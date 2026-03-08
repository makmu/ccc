using CCC.Infrastructure.EventStore;

namespace CCC.Users;

static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapPost("/users", async (AddUserRequest request, CommandContextBuilder contextBuilder) =>
        {
            var context = await contextBuilder
                .Where<UserAdded>(w => w
                    .With(e => e.Email, request.Email)
                    .Or(e => e.Id, request.Id))
                .LoadAsync();

            if (context.Events.Count != 0)
                return Results.Conflict("A user with this email address or id already exists.");

            try
            {
                await context.AppendAsync(new UserAdded(request.Id, request.Name, request.Email));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
