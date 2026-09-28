using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.ValueObjects;
using CCC.EventStore;

namespace CCC.Api.Tenants;

static class TenantEndpoints
{

    public static void MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/tenants", async (RegisterTenantRequest request, IStateLoader loader) =>
        {
            if (request.SubscriptionModelIds.Count == 0)
                return Results.BadRequest("At least one subscription model must be referenced.");

            var tenantId = TenantId.Create(request.Id);
            var tenantName = TenantName.Create(request.Name);
            var subscriptionModelIds = request.SubscriptionModelIds.Select(SubscriptionModelId.Create).ToArray();

            var tenantIdTaken = State.TenantExists(tenantId);
            var tenantNameTaken = State.TenantNameTaken(tenantName);

            var subscriptionsAvailable = subscriptionModelIds
                .Select(State.SubscriptionExists).ToArray();

            var context = await loader.LoadAsync([tenantIdTaken, tenantNameTaken, ..subscriptionsAvailable]);

            if (tenantIdTaken)
                return Results.Conflict("A tenant with this id already exists.");

            if (tenantNameTaken)
                return Results.Conflict("A tenant with this name already exists.");

            if (subscriptionsAvailable.Any(x => !x))
                return Results.NotFound("One or more subscription models were not found.");

            try
            {
                await context.AppendAsync(new TenantRegistered(request.Id, request.Name, request.SubscriptionModelIds),
                    Tags.From(Tag.TenantId(tenantId), Tag.TenantName(tenantName)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/teams", async (Guid tenantId, AddTeamRequest request, IStateLoader loader) =>
        {
            var tenant = TenantId.Create(tenantId);
            var teamId = TeamId.Create(request.Id);
            var teamName = TeamName.Create(request.Name);
            var subscriptionModelId = SubscriptionModelId.Create(request.SubscriptionModelId);

            var tenantIdRegistered = State.TenantExists(tenant);
            var tenantSubscriptions = State.TenantSubscriptions(tenant);
            var teamIdTaken = State.TeamIdTaken(tenant, teamId);
            var teamNameConflict = State.TeamNameTaken(tenant, teamName);

            var context = await loader.LoadAsync(tenantIdRegistered, tenantSubscriptions, teamIdTaken, teamNameConflict);

            if (!tenantIdRegistered)
                return Results.NotFound("Tenant not found.");

            if (!tenantSubscriptions.State.Contains(subscriptionModelId.Value))
                return Results.BadRequest("The specified subscription model is not available in this tenant.");

            if (teamIdTaken || teamNameConflict)
                return Results.Conflict("A team with this id or name already exists in this tenant.");

            try
            {
                await context.AppendAsync(new TeamAdded(request.Id, request.Name, tenantId, request.SubscriptionModelId),
                    Tags.From(Tag.TeamId(teamId), Tag.TenantId(tenant), Tag.TeamName(teamName)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/owners", async (Guid tenantId, AssignTenantOwnerRequest request, IStateLoader loader) =>
        {
            var tenant = TenantId.Create(tenantId);
            var userId = UserId.Create(request.UserId);

            var tenantIdRegistered = State.TenantExists(tenant);
            var userAdded = State.UserExists(userId);
            var userAlreadyAssigned = State.UserIsTenantOwner(userId, tenant);

            var context = await loader.LoadAsync(tenantIdRegistered, userAdded, userAlreadyAssigned);

            if (!tenantIdRegistered)
                return Results.NotFound("Tenant not found.");

            if (!userAdded)
                return Results.NotFound("User not found.");

            if (userAlreadyAssigned)
                return Results.Conflict("This user is already an owner of this tenant.");

            try
            {
                await context.AppendAsync(new TenantOwnerAssigned(request.UserId, tenantId),
                    Tags.From(Tag.TenantId(tenant), Tag.UserId(userId)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/rename", async (Guid tenantId, RenameTenantRequest request, IStateLoader loader) =>
        {
            var tenant = TenantId.Create(tenantId);
            var newName = TenantName.Create(request.Name);

            var tenantIdRegistered = State.TenantExists(tenant);
            var currentTenantName = State.CurrentTenantName(tenant);
            var tenantNameTaken = State.TenantNameTaken(newName);

            var context = await loader.LoadAsync(tenantIdRegistered, currentTenantName, tenantNameTaken);

            // Decisions
            if (!tenantIdRegistered)
                return Results.NotFound("Tenant not found.");

            // Case-insensitive no-op: renaming to the same name (any casing) changes nothing.
            var currentName = TenantName.Create(currentTenantName.State);
            if (currentName.Canonical == newName.Canonical)
                return Results.Ok();

            if (tenantNameTaken)
                return Results.Conflict("A tenant with this name already exists.");

            // Append event — tag both names (canonical) under the same key so future
            // name lookups match this rename whether it took or freed a name.
            try
            {
                await context.AppendAsync(new TenantRenamed(tenantId, request.Name), Tags.From(
                    Tag.TenantId(tenant),
                    Tag.TenantName(currentName),
                    Tag.TenantName(newName)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
