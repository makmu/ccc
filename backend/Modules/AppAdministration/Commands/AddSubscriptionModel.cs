using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.AppAdministration.Commands;

public record AddSubscriptionModel(SubscriptionModelId Id, SubscriptionModelName Name, KeyLimit KeyLimit);

sealed class AddSubscriptionModelHandler(IStateLoader loader) : ICommandHandler<AddSubscriptionModel>
{
    public async Task<CommandResult> HandleAsync(AddSubscriptionModel command, CancellationToken cancellationToken)
    {
        var idTaken = Subscription.Exists(command.Id);
        var context = await loader.LoadAsync([idTaken], cancellationToken);

        if (idTaken.IsTrue())
            return CommandResult.Conflict("A subscription model with this id already exists.");

        var appended = await context.TryAppendAsync(
            new SubscriptionModelAdded(command.Id.Value, command.Name.Value, command.KeyLimit.Value),
            Tags.From(Tag.SubscriptionId(command.Id)),
            cancellationToken);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
