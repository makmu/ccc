using System.Text.Json;
using CCC.Infrastructure.EventStore;
using CCC.SubscriptionModels;
using CCC.Users;

namespace CCC.Tenants;

static class TenantEndpoints
{
    public static void MapTenantEndpoints(this WebApplication app)
    {
        app.MapPost("/tenants", async (RegisterTenantRequest request, CommandContextBuilder contextBuilder) =>
        {
            if (request.SubscriptionModelIds.Count == 0)
                return Results.BadRequest("At least one subscription model must be referenced.");

            var context = await contextBuilder
                .Where<TenantRegistered>(w => w
                    .With(e => e.Id, request.Id)
                    .Or(e => e.Name, request.Name))
                .Where<SubscriptionModelAdded>()
                .LoadAsync();

            if (context.Events.Any(e => e.Type == nameof(TenantRegistered)))
                return Results.Conflict("A tenant with this id or name already exists.");

            var existingModelIds = context.Events
                .Where(e => e.Type == nameof(SubscriptionModelAdded))
                .Select(e => JsonSerializer.Deserialize<SubscriptionModelAdded>(e.Payload)!.Id)
                .ToHashSet();

            if (request.SubscriptionModelIds.Any(id => !existingModelIds.Contains(id)))
                return Results.NotFound("One or more subscription models were not found.");

            try
            {
                await context.AppendAsync(new TenantRegistered(request.Id, request.Name, request.SubscriptionModelIds));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/teams", async (Guid tenantId, AddTeamRequest request, CommandContextBuilder contextBuilder) =>
        {
            var context = await contextBuilder
                .Where<TenantRegistered>(w => w.With(e => e.Id, tenantId))
                .Where<TeamAdded>(w => w.With(e => e.TenantId, tenantId))
                .LoadAsync();

            var tenant = context.Events
                .Where(e => e.Type == nameof(TenantRegistered))
                .Select(e => JsonSerializer.Deserialize<TenantRegistered>(e.Payload)!)
                .SingleOrDefault();

            if (tenant is null)
                return Results.NotFound("Tenant not found.");

            if (!tenant.SubscriptionModelIds.Contains(request.SubscriptionModelId))
                return Results.BadRequest("The specified subscription model is not available in this tenant.");

            var conflict = context.Events
                .Where(e => e.Type == nameof(TeamAdded))
                .Select(e => JsonSerializer.Deserialize<TeamAdded>(e.Payload)!)
                .Any(t => t.Id == request.Id || t.Name == request.Name);

            if (conflict)
                return Results.Conflict("A team with this id or name already exists in this tenant.");

            try
            {
                await context.AppendAsync(new TeamAdded(request.Id, request.Name, tenantId, request.SubscriptionModelId));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/owners", async (Guid tenantId, AssignTenantOwnerRequest request, CommandContextBuilder contextBuilder) =>
        {
            var context = await contextBuilder
                .Where<TenantRegistered>(w => w.With(e => e.Id, tenantId))
                .Where<UserAdded>(w => w.With(e => e.Id, request.UserId))
                .Where<TenantOwnerAssigned>(w => w.With(e => e.TenantId, tenantId))
                .LoadAsync();

            if (context.Events.All(e => e.Type != nameof(TenantRegistered)))
                return Results.NotFound("Tenant not found.");

            if (context.Events.All(e => e.Type != nameof(UserAdded)))
                return Results.NotFound("User not found.");

            var alreadyAssigned = context.Events
                .Where(e => e.Type == nameof(TenantOwnerAssigned))
                .Select(e => JsonSerializer.Deserialize<TenantOwnerAssigned>(e.Payload)!)
                .Any(o => o.UserId == request.UserId);

            if (alreadyAssigned)
                return Results.Conflict("This user is already an owner of this tenant.");

            try
            {
                await context.AppendAsync(new TenantOwnerAssigned(request.UserId, tenantId));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });

        app.MapPost("/tenants/{tenantId}/rename", async (Guid tenantId, RenameTenantRequest request, CommandContextBuilder contextBuilder) =>
        {
            // Context
            var context = await contextBuilder
                .Where<TenantRegistered>()
                .Where<TenantRenamed>()
                .LoadAsync();

            // State Slice
            var currentNames = new Dictionary<Guid, string>();
            foreach (var e in context.Events)
            {
                if (e.Type == nameof(TenantRegistered))
                {
                    var added = JsonSerializer.Deserialize<TenantRegistered>(e.Payload)!;
                    currentNames[added.Id] = added.Name;
                }
                else
                {
                    var renamed = JsonSerializer.Deserialize<TenantRenamed>(e.Payload)!;
                    currentNames[renamed.TenantId] = renamed.Name;
                }
            }

            // Decisions
            if (!currentNames.ContainsKey(tenantId))
                return Results.NotFound("Tenant not found.");

            if (currentNames.Any(o => o.Key != tenantId && o.Value == request.Name))
                return Results.Conflict("A tenant with this name already exists.");

            // Append event
            try
            {
                await context.AppendAsync(new TenantRenamed(tenantId, request.Name));
            }
            catch (ConcurrencyException)
            {
                return Results.Conflict("A concurrent operation modified the event store. Please retry.");
            }

            return Results.Ok();
        });
    }
}
