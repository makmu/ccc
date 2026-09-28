using CCC.Domain;
using CCC.Domain.Events;
using CCC.Helpers;
using CCC.EventStore;

namespace CCC.Api.Tenants;

static class TenantEndpoints
{

    public static void MapTenantEndpoints(this WebApplication app)
    {
        app.MapPost("/tenants", async (RegisterTenantRequest request, IStateLoader loader) =>
        {
            if (request.SubscriptionModelIds.Count == 0)
                return Results.BadRequest("At least one subscription model must be referenced.");

            var tenantIdTaken = State.TenantExists(request.Id);

            var canonicalName = request.Name.CanonicalName();
            var tenantNameTaken = State.TenantNameTaken(request.Name);

            var subscriptionsAvailable = request.SubscriptionModelIds
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
                    Tags.From(Tag.TenantId(request.Id), Tag.TenantName(canonicalName)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/teams", async (Guid tenantId, AddTeamRequest request, IStateLoader loader) =>
        {
            var tenantIdRegistered = State.TenantExists(tenantId);
            var tenantSubscriptions = State.TenantSubscriptions(tenantId);
            var teamIdTaken = State.TeamIdTaken(tenantId, request.Id);

            var canonicalName = request.Name.CanonicalName();
            var teamNameConflict = State.TeamNameTaken(tenantId, request.Name);

            var context = await loader.LoadAsync(tenantIdRegistered, tenantSubscriptions, teamIdTaken, teamNameConflict);

            if (!tenantIdRegistered)
                return Results.NotFound("Tenant not found.");

            if (!tenantSubscriptions.State.Contains(request.SubscriptionModelId))
                return Results.BadRequest("The specified subscription model is not available in this tenant.");

            if (teamIdTaken || teamNameConflict)
                return Results.Conflict("A team with this id or name already exists in this tenant.");

            try
            {
                await context.AppendAsync(new TeamAdded(request.Id, request.Name, tenantId, request.SubscriptionModelId),
                Tags.From(Tag.TeamId(request.Id), Tag.TenantId(tenantId), Tag.TeamName(canonicalName)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/owners", async (Guid tenantId, AssignTenantOwnerRequest request, IStateLoader loader) =>
        {
            var tenantIdRegistered = State.TenantExists(tenantId);
            var userAdded = State.UserExists(request.UserId);
            var userAlreadyAssigned = State.UserIsTenantOwner(request.UserId, tenantId);

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
                Tags.From(Tag.TenantId(tenantId), Tag.UserId(request.UserId)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/rename", async (Guid tenantId, RenameTenantRequest request, IStateLoader loader) =>
        {
            var tenantIdRegistered = State.TenantExists(tenantId);
            var currentTenantName = State.CurrentTenantName(tenantId);
            var tenantNameTaken = State.TenantNameTaken(request.Name);

            var context = await loader.LoadAsync(tenantIdRegistered, currentTenantName, tenantNameTaken);

            // Decisions
            if (!tenantIdRegistered)
                return Results.NotFound("Tenant not found.");

            // Case-insensitive no-op: renaming to the same name (any casing) changes nothing.
            var canonicalName = request.Name.CanonicalName();
            if (currentTenantName.State.CanonicalName() == canonicalName)
                return Results.Ok();

            if (tenantNameTaken)
                return Results.Conflict("A tenant with this name already exists.");

            // Append event — tag both names (canonical) under the same key so future
            // name lookups match this rename whether it took or freed a name.
            try
            {
                await context.AppendAsync(new TenantRenamed(tenantId, request.Name), Tags.From(
                    Tag.TenantId(tenantId),
                    Tag.TenantName(currentTenantName.State.CanonicalName()),
                    Tag.TenantName(canonicalName)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
