using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.EventStore;

namespace CCC.Commands.SubscriptionModels;

sealed class AddSubscriptionModelHandler(IStateLoader loader) : ICommandHandler<AddSubscriptionModel>
{
    public async Task<CommandResult> HandleAsync(AddSubscriptionModel command)
    {
        var subscriptionIdTaken = State.SubscriptionExists(command.Id);

        var context = await loader.LoadAsync(subscriptionIdTaken);

        if (subscriptionIdTaken)
            return CommandResult.Conflict("A subscription model with this id already exists.");

        return await context.TryAppendAsync(
            new SubscriptionModelAdded(command.Id.Value, command.Name, command.KeyLimit.Value),
            Tags.From(Tag.SubscriptionId(command.Id)));
    }
}
