using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.AppAdministration.Commands;

public record RegisterTenant(TenantId Id, TenantName Name, IReadOnlyList<SubscriptionModelId> SubscriptionModelIds);

sealed class RegisterTenantHandler(IStateLoader loader) : ICommandHandler<RegisterTenant>
{
    public async Task<CommandResult> HandleAsync(RegisterTenant command, CancellationToken cancellationToken)
    {
        if (command.SubscriptionModelIds.Count == 0)
            return CommandResult.Invalid("At least one subscription model must be referenced.");

        var idTaken = Tenant.Exists(command.Id);
        var nameTaken = Tenant.NameTaken(command.Name);
        var subscriptionsExist = Subscription.AllExist(command.SubscriptionModelIds);
        var context = await loader.LoadAsync([idTaken, nameTaken, subscriptionsExist], cancellationToken);

        if (idTaken.IsTrue())
            return CommandResult.Conflict("A tenant with this id already exists.");

        if (nameTaken.IsTrue())
            return CommandResult.Conflict("A tenant with this name already exists.");

        if (subscriptionsExist.IsFalse())
            return CommandResult.NotFound("One or more subscription models were not found.");

        var appended = await context.TryAppendAsync(
            new TenantRegistered(command.Id.Value, command.Name.Value, command.SubscriptionModelIds.Select(id => id.Value).Distinct().ToList()),
            Tags.From(Tag.TenantId(command.Id), Tag.TenantName(command.Name)),
            cancellationToken);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
