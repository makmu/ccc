using CCC.Infrastructure.EventStore;

namespace CCC.Organizations;

static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this WebApplication app)
    {
        app.MapPost("/organizations", async (AddOrganizationRequest request, CommandContextBuilder contextBuilder) =>
        {
            var context = await contextBuilder
                .Where<OrganizationAdded>(w => w
                    .Or(e => e.Id, request.Id)
                    .Or(e => e.Name, request.Name))
                .LoadAsync();

            if (context.Events.Count != 0)
                return Results.Conflict("An organization with this id or name already exists.");

            try
            {
                await context.AppendAsync(new OrganizationAdded(request.Id, request.Name));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
