using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.EventStore;

namespace CCC.Commands.Tenants;

sealed class RegisterTenantHandler(IStateLoader loader) : ICommandHandler<RegisterTenant>
{
    public async Task<CommandResult> HandleAsync(RegisterTenant command)
    {
        if (command.SubscriptionModelIds.Count == 0)
            return CommandResult.Invalid("At least one subscription model must be referenced.");

        var tenantIdTaken = State.TenantExists(command.Id);
        var tenantNameTaken = State.TenantNameTaken(command.Name);

        var subscriptionsAvailable = command.SubscriptionModelIds
            .Select(State.SubscriptionExists).ToArray();

        var context = await loader.LoadAsync([tenantIdTaken, tenantNameTaken, ..subscriptionsAvailable]);

        if (tenantIdTaken)
            return CommandResult.Conflict("A tenant with this id already exists.");

        if (tenantNameTaken)
            return CommandResult.Conflict("A tenant with this name already exists.");

        if (subscriptionsAvailable.Any(x => !x))
            return CommandResult.NotFound("One or more subscription models were not found.");

        return await context.TryAppendAsync(
            new TenantRegistered(command.Id.Value, command.Name.Value, command.SubscriptionModelIds.Select(id => id.Value).ToList()),
            Tags.From(Tag.TenantId(command.Id), Tag.TenantName(command.Name)));
    }
}
