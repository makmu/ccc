using CCC.Infrastructure.EventStore;

namespace CCC.Users;

static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapPost("/users", async (AddUserRequest request, CommandContextBuilder contextBuilder) =>
        {
            // Two decision models, one query pass. Subset tag matching means each
            // uniqueness rule needs its own tag slice.
            var idTaken = StateModel
                .Init(false)
                .Where("user-id", request.Id.ToString())
                .When<UserAdded>((_, _) => true);

            var emailTaken = StateModel
                .Init(false)
                .Where("user-email", request.Email)
                .When<UserAdded>((_, _) => true);

            var context = await contextBuilder.BuildStateModelsAsync(idTaken, emailTaken);

            // decision
            if (idTaken.State)
                return Results.Conflict("A user with this id already exists.");
            if (emailTaken.State)
                return Results.Conflict("A user with this email address already exists.");

            // write decision result — tag the event so future models can find it
            try
            {
                await context.AppendAsync(
                    new UserAdded(request.Id, request.Name, request.Email),
                    Tags.From(("user-id", request.Id.ToString()), ("user-email", request.Email)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
