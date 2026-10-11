using CCC.Dcb;
using CCC.Domain.Events;
using CCC.Domain.Types;

namespace CCC.Domain.State;

public static class Subscription
{
    public static StateModel<bool> Exists(SubscriptionModelId subscriptionId) =>
        StateModel.Init(false)
            .Where(Tag.SubscriptionId(subscriptionId))
            .When<SubscriptionModelAdded>((_, _) => true);

    public static IStateModel<bool> AllExist(IEnumerable<SubscriptionModelId> subscriptionIds) =>
        StateModel.All(subscriptionIds.Select(Exists));

    public static StateModel<SubscriptionModelName?> Name(SubscriptionModelId subscriptionId) =>
        StateModel.Init<SubscriptionModelName?>(null)
            .Where(Tag.SubscriptionId(subscriptionId))
            .When<SubscriptionModelAdded>((e, _) => SubscriptionModelName.From(e.Name));

    // Inside this class, a bare KeyLimit means this method, so the type is qualified.
    public static StateModel<KeyLimit> KeyLimit(SubscriptionModelId subscriptionId) =>
        StateModel.Init(Types.KeyLimit.From(0))
            .Where(Tag.SubscriptionId(subscriptionId))
            .When<SubscriptionModelAdded>((e, _) => Types.KeyLimit.From(e.KeyLimit));
}
