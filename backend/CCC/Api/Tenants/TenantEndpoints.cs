using System.Text.Json;
using CCC.Domain;
using CCC.Domain.Events;
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

            var tenantIdTaken = Model.TenantExists(request.Id);

            var canonicalName = request.Name.CanonicalName();
            var tenantNameTaken = Model.TenantNameTaken(request.Name);

            var subscriptionsAvailable = request.SubscriptionModelIds
                .Select(Model.SubscriptionExists).ToArray();

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
                    Tags.From(Tag.TenantId(request.Id), Tag.TenantName(canonicalName)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/teams", async (Guid tenantId, AddTeamRequest request, CommandContextBuilder contextBuilder) =>
        {
            var tenantIdRegistered = Model.TenantExists(tenantId);
            var tenantSubscriptions = Model.TenantSubscriptions(tenantId);
            var teamIdTaken = Model.TeamIdTaken(tenantId, request.Id);

            var canonicalName = request.Name.CanonicalName();
            var teamNameConflict = Model.TeamNameTaken(tenantId, request.Name);

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
                Tags.From(Tag.TeamId(request.Id), Tag.TenantId(tenantId), Tag.TeamName(canonicalName)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/owners", async (Guid tenantId, AssignTenantOwnerRequest request, CommandContextBuilder contextBuilder) =>
        {
            var tenantIdRegistered = Model.TenantExists(tenantId);
            var userAdded = Model.UserExists(request.UserId);
            var userAlreadyAssigned = Model.UserIsTenantOwner(request.UserId, tenantId);

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
                Tags.From(Tag.TenantId(tenantId), Tag.UserId(request.UserId)));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/rename", async (Guid tenantId, RenameTenantRequest request, CommandContextBuilder contextBuilder) =>
        {
            var tenantIdRegistered = Model.TenantExists(tenantId);
            var currentTenantName = Model.CurrentTenantName(tenantId);
            var canonicalName = request.Name.CanonicalName();
            var tenantNameTaken = Model.TenantNameTaken(request.Name);

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
