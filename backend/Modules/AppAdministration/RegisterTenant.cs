using CCC.Abstractions;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.AppAdministration;

public record RegisterTenant(TenantId Id, TenantName Name, IReadOnlyList<SubscriptionModelId> SubscriptionModelIds);

sealed class RegisterTenantHandler(IStateLoader loader) : ICommandHandler<RegisterTenant>
{
    public async Task<CommandResult> HandleAsync(RegisterTenant command)
    {
        if (command.SubscriptionModelIds.Count == 0)
            return CommandResult.Invalid("At least one subscription model must be referenced.");

        var (idTaken, nameTaken, subscriptionsExist, context) = await loader.LoadAsync(
            Tenant.Exists(command.Id),
            Tenant.NameTaken(command.Name),
            Subscription.AllExist(command.SubscriptionModelIds));

        if (idTaken)
            return CommandResult.Conflict("A tenant with this id already exists.");

        if (nameTaken)
            return CommandResult.Conflict("A tenant with this name already exists.");

        if (!subscriptionsExist)
            return CommandResult.NotFound("One or more subscription models were not found.");

        var appended = await context.TryAppendAsync(
            new TenantRegistered(command.Id.Value, command.Name.Value, command.SubscriptionModelIds.Select(id => id.Value).ToList()),
            Tags.From(Tag.TenantId(command.Id), Tag.TenantName(command.Name)));
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
