using System.Text.Json;
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
                    .With(e => e.Id, request.Id)
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

        app.MapPost("/organizations/{organizationId}/teams", async (Guid organizationId, AddTeamRequest request, CommandContextBuilder contextBuilder) =>
        {
            var context = await contextBuilder
                .Where<OrganizationAdded>(w => w.With(e => e.Id, organizationId))
                .Where<TeamAdded>(w => w.With(e => e.OrganizationId, organizationId))
                .LoadAsync();

            if (context.Events.All(e => e.Type != nameof(OrganizationAdded)))
                return Results.NotFound("Organization not found.");

            var conflict = context.Events
                .Where(e => e.Type == nameof(TeamAdded))
                .Select(e => JsonSerializer.Deserialize<TeamAdded>(e.Payload)!)
                .Any(t => t.Id == request.Id || t.Name == request.Name);

            if (conflict)
                return Results.Conflict("A team with this id or name already exists in this organization.");

            try
            {
                await context.AppendAsync(new TeamAdded(request.Id, request.Name, organizationId));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
