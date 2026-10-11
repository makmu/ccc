using CCC.Dcb;
using CCC.Domain.Events;
using CCC.Domain.Types;

namespace CCC.Domain.State;

public static class Team
{
    public static StateModel<bool> Exists(TeamId teamId) =>
        StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .When<TeamAdded>((_, _) => true);

    public static StateModel<bool> IdTaken(TeamId teamId) =>
        StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .When<TeamAdded>((_, _) => true);

    public static StateModel<bool> NameTaken(TenantId tenantId, TeamName name) =>
        StateModel.Init(false)
            .Where(Tag.TenantId(tenantId))
            .Where(Tag.TeamName(name))
            .When<TeamAdded>((_, _) => true);

    public static StateModel<SubscriptionModelId?> Subscription(TeamId teamId) =>
        StateModel.Init<SubscriptionModelId?>(null)
            .Where(Tag.TeamId(teamId))
            .When<TeamAdded>((e, _) => SubscriptionModelId.From(e.SubscriptionModelId));

    public static StateModel<KeyCount> KeysAdded(TeamId teamId) =>
        StateModel.Init(KeyCount.Zero)
            .Where(Tag.TeamId(teamId))
            .When<KeyAdded>((_, count) => count.Increment());
}
