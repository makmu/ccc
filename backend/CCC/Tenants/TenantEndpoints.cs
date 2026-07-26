using System.Text.Json;
using CCC.Helpers;
using CCC.Infrastructure.EventStore;
using CCC.SubscriptionModels;
using CCC.Users;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CCC.Tenants;

static class TenantEndpoints
{

    public static void MapTenantEndpoints(this WebApplication app)
    {
        app.MapPost("/tenants", async (RegisterTenantRequest request, CommandContextBuilder contextBuilder) =>
        {
            if (request.SubscriptionModelIds.Count == 0)
                return Results.BadRequest("At least one subscription model must be referenced.");

            var tenantIdTaken = StateModel.Init(false)
                .Where("tenant-id", request.Id.ToString())
                .When<TenantRegistered>((_, _) => true);

            var canonicalName = request.Name.CanonicalName();
            var tenantNameTaken = StateModel.Init(false)
                .Where("tenant-name", canonicalName)
                .When<TenantRegistered>((_, _) => true)
                .When<TenantRenamed>((e, _) => e.Name.CanonicalName() == canonicalName);

            var subscriptionsAvailable = request.SubscriptionModelIds.Select(x => StateModel.Init(false)
                .Where("subscription-id", x.ToString())
                .When<SubscriptionModelAdded>((_, _) => true)).ToArray();

            var context = await contextBuilder.BuildStateModelsAsync([tenantIdTaken, tenantNameTaken, ..subscriptionsAvailable]);

            if(tenantIdTaken.State)
                return Results.Conflict("A tenant with this id already exists.");

            if(tenantNameTaken.State)
                return Results.Conflict("A tenant with this name already exists.");

            if(subscriptionsAvailable.Any(x => !x.State))
                return Results.NotFound("One or more subscription models were not found.");

            try
            {
                await context.AppendAsync(new TenantRegistered(request.Id, request.Name, request.SubscriptionModelIds),
                    Tags.From(("tenant-id", request.Id.ToString()), ("tenant-name", canonicalName)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/teams", async (Guid tenantId, AddTeamRequest request, CommandContextBuilder contextBuilder) =>
        {
            var  tenantIdRegistered = StateModel.Init(false)
                .Where("tenant-id", tenantId.ToString())
                .When<TenantRegistered>((_, _) => true);
            
            var tenantSubscriptions = StateModel.Init(Array.Empty<Guid>())
                .Where("tenant-id", tenantId.ToString())
                .When<TenantRegistered>((e, s) => e.SubscriptionModelIds.ToArray());
            
            var teamIdTaken = StateModel.Init(false)
                .Where("tenant-id", tenantId.ToString())
                .Where("team-id", request.Id.ToString())
                .When<TeamAdded>((_, _) => true);
            
            var canonicalName = request.Name.CanonicalName();
            var teamNameConflict = StateModel.Init(false)
                .Where("tenant-id", tenantId.ToString())
                .Where("team-name", canonicalName)
                .When<TeamAdded>((_, _) => true);

            var context = await contextBuilder.BuildStateModelsAsync(tenantIdRegistered, tenantSubscriptions, teamIdTaken, teamNameConflict);

            if (!tenantIdRegistered.State)
                return Results.NotFound("Tenant not found.");

            if (!tenantSubscriptions.State.Contains(request.SubscriptionModelId))
                return Results.BadRequest("The specified subscription model is not available in this tenant.");

            if (teamIdTaken.State || teamNameConflict.State)
                return Results.Conflict("A team with this id or name already exists in this tenant.");

            try
            {
                await context.AppendAsync(new TeamAdded(request.Id, request.Name, tenantId, request.SubscriptionModelId),
                Tags.From(("team-id", request.Id.ToString()), ("tenant-id", tenantId.ToString()), ("team-name", canonicalName)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/owners", async (Guid tenantId, AssignTenantOwnerRequest request, CommandContextBuilder contextBuilder) =>
        {
            var tenantIdRegistered = StateModel.Init(false)
                .Where("tenant-id", tenantId.ToString())
                .When<TenantRegistered>((_, _) => true);
            var userAdded = StateModel.Init(false)
                .Where("user-id", request.UserId.ToString())
                .When<UserAdded>((_, _) => true);
            var userAlreadyAssigned = StateModel.Init(false)
                .Where("user-id", request.UserId.ToString())
                .Where("tenant-id", tenantId.ToString())
                .When<TenantOwnerAssigned>((_, _) => true);

            var context = await contextBuilder.BuildStateModelsAsync(tenantIdRegistered, userAdded, userAlreadyAssigned);
            
            if(!tenantIdRegistered.State)
                return Results.NotFound("Tenant not found.");

            if (!userAdded.State)
                return Results.NotFound("User not found.");

            if (userAlreadyAssigned.State)
                return Results.Conflict("This user is already an owner of this tenant.");

            try
            {
                await context.AppendAsync(new TenantOwnerAssigned(request.UserId, tenantId),
                Tags.From(("tenant-id", tenantId.ToString()), ("user-id", request.UserId.ToString())));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/rename", async (Guid tenantId, RenameTenantRequest request, CommandContextBuilder contextBuilder) =>
        {
            var tenantIdRegistered = StateModel.Init(false)
                .Where("tenant-id", tenantId.ToString())
                .When<TenantRegistered>((_, _) => true);

            // This tenant's name history, folded to its latest name.
            var currentTenantName = StateModel.Init(string.Empty)
                .Where("tenant-id", tenantId.ToString())
                .When<TenantRegistered>((e, _) => e.Name)
                .When<TenantRenamed>((e, _) => e.Name);

            // Is the target name currently taken by anyone? Matches every event that
            // touches the name (registered as / renamed to / renamed from); the
            // reducer decides whether it ends up taken or freed.
            var canonicalName = request.Name.CanonicalName();
            var tenantNameTaken = StateModel.Init(false)
                .Where("tenant-name", canonicalName)
                .When<TenantRegistered>((_, _) => true)
                .When<TenantRenamed>((e, _) => e.Name.CanonicalName() == canonicalName);

            var context = await contextBuilder.BuildStateModelsAsync(tenantIdRegistered, currentTenantName, tenantNameTaken);

            // Decisions
            if(!tenantIdRegistered.State)
                return Results.NotFound("Tenant not found.");

            // Case-insensitive no-op: renaming to the same name (any casing) changes nothing.
            if (currentTenantName.State.CanonicalName() == canonicalName)
                return Results.Ok();

            if(tenantNameTaken.State)
                return Results.Conflict("A tenant with this name already exists.");

            // Append event — tag both names (canonical) under the same key so future
            // name lookups match this rename whether it took or freed a name.
            try
            {
                await context.AppendAsync(new TenantRenamed(tenantId, request.Name), Tags.From(
                    ("tenant-id", tenantId.ToString()),
                    ("tenant-name", currentTenantName.State.CanonicalName()),
                    ("tenant-name", canonicalName)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
