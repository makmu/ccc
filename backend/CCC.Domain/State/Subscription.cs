using CCC.Domain.Events;
using CCC.Domain.ValueObjects;
using CCC.EventStore;

namespace CCC.Domain.State;

public static class Subscription
{
    public static StateModel<bool> Exists(SubscriptionModelId subscriptionId) =>
        StateModel.Init(false)
            .Where(Tag.SubscriptionId(subscriptionId))
            .When<SubscriptionModelAdded>((_, _) => true);

    public static StateModel<int> KeyLimit(SubscriptionModelId subscriptionId) =>
        StateModel.Init(0)
            .Where(Tag.SubscriptionId(subscriptionId))
            .When<SubscriptionModelAdded>((e, _) => e.KeyLimit);
}
