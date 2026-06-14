using System.Text.Json;
using CCC.Infrastructure.EventStore;
using CCC.SubscriptionModels;
using CCC.Users;

namespace CCC.Organizations;

static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this WebApplication app)
    {
        app.MapPost("/organizations", async (AddOrganizationRequest request, CommandContextBuilder contextBuilder) =>
        {
            if (request.SubscriptionModelIds.Count == 0)
                return Results.BadRequest("At least one subscription model must be referenced.");

            var context = await contextBuilder
                .Where<OrganizationAdded>(w => w
                    .With(e => e.Id, request.Id)
                    .Or(e => e.Name, request.Name))
                .Where<SubscriptionModelAdded>()
                .LoadAsync();

            if (context.Events.Any(e => e.Type == nameof(OrganizationAdded)))
                return Results.Conflict("An organization with this id or name already exists.");

            var existingModelIds = context.Events
                .Where(e => e.Type == nameof(SubscriptionModelAdded))
                .Select(e => JsonSerializer.Deserialize<SubscriptionModelAdded>(e.Payload)!.Id)
                .ToHashSet();

            if (request.SubscriptionModelIds.Any(id => !existingModelIds.Contains(id)))
                return Results.NotFound("One or more subscription models were not found.");

            try
            {
                await context.AppendAsync(new OrganizationAdded(request.Id, request.Name, request.SubscriptionModelIds));
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

            var organization = context.Events
                .Where(e => e.Type == nameof(OrganizationAdded))
                .Select(e => JsonSerializer.Deserialize<OrganizationAdded>(e.Payload)!)
                .SingleOrDefault();

            if (organization is null)
                return Results.NotFound("Organization not found.");

            if (!organization.SubscriptionModelIds.Contains(request.SubscriptionModelId))
                return Results.BadRequest("The specified subscription model is not available in this organization.");

            var conflict = context.Events
                .Where(e => e.Type == nameof(TeamAdded))
                .Select(e => JsonSerializer.Deserialize<TeamAdded>(e.Payload)!)
                .Any(t => t.Id == request.Id || t.Name == request.Name);

            if (conflict)
                return Results.Conflict("A team with this id or name already exists in this organization.");

            try
            {
                await context.AppendAsync(new TeamAdded(request.Id, request.Name, organizationId, request.SubscriptionModelId));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/organizations/{organizationId}/owners", async (Guid organizationId, AssignOrganizationOwnerRequest request, CommandContextBuilder contextBuilder) =>
        {
            var context = await contextBuilder
                .Where<OrganizationAdded>(w => w.With(e => e.Id, organizationId))
                .Where<UserAdded>(w => w.With(e => e.Id, request.UserId))
                .Where<OrganizationOwnerAssigned>(w => w.With(e => e.OrganizationId, organizationId))
                .LoadAsync();

            if (context.Events.All(e => e.Type != nameof(OrganizationAdded)))
                return Results.NotFound("Organization not found.");

            if (context.Events.All(e => e.Type != nameof(UserAdded)))
                return Results.NotFound("User not found.");

            var alreadyAssigned = context.Events
                .Where(e => e.Type == nameof(OrganizationOwnerAssigned))
                .Select(e => JsonSerializer.Deserialize<OrganizationOwnerAssigned>(e.Payload)!)
                .Any(o => o.UserId == request.UserId);

            if (alreadyAssigned)
                return Results.Conflict("This user is already an owner of this organization.");

            try
            {
                await context.AppendAsync(new OrganizationOwnerAssigned(request.UserId, organizationId));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/organizations/{organizationId}/rename", async (Guid organizationId, RenameOrganizationRequest request, CommandContextBuilder contextBuilder) =>
        {
            // Context
            var context = await contextBuilder
                .Where<OrganizationAdded>()
                .Where<OrganizationRenamed>()
                .LoadAsync();

            // State Slice
            var currentNames = new Dictionary<Guid, string>();
            foreach (var e in context.Events)
            {
                if (e.Type == nameof(OrganizationAdded))
                {
                    var added = JsonSerializer.Deserialize<OrganizationAdded>(e.Payload)!;
                    currentNames[added.Id] = added.Name;
                }
                else
                {
                    var renamed = JsonSerializer.Deserialize<OrganizationRenamed>(e.Payload)!;
                    currentNames[renamed.OrganizationId] = renamed.Name;
                }
            }

            // Descisions
            if (!currentNames.ContainsKey(organizationId))
                return Results.NotFound("Organization not found.");

            if (currentNames.Any(o => o.Key != organizationId && o.Value == request.Name))
                return Results.Conflict("An organization with this name already exists.");

            // Append event
            try
            {
                await context.AppendAsync(new OrganizationRenamed(organizationId, request.Name));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
