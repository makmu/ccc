using CCC.Domain.Events;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Domain.State;

public static class Team
{
    public static StateModel<bool> Exists(TeamId teamId) =>
        StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .When<TeamAdded>((_, _) => true);

    public static StateModel<bool> IdTaken(TenantId tenantId, TeamId teamId) =>
        StateModel.Init(false)
            .Where(Tag.TenantId(tenantId))
            .Where(Tag.TeamId(teamId))
            .When<TeamAdded>((_, _) => true);

    public static StateModel<bool> NameTaken(TenantId tenantId, TeamName name) =>
        StateModel.Init(false)
            .Where(Tag.TenantId(tenantId))
            .Where(Tag.TeamName(name))
            .When<TeamAdded>((_, _) => true);

    public static StateModel<Guid?> Subscription(TeamId teamId) =>
        StateModel.Init<Guid?>(null)
            .Where(Tag.TeamId(teamId))
            .When<TeamAdded>((e, _) => e.SubscriptionModelId);

    public static StateModel<int> KeyCount(TeamId teamId) =>
        StateModel.Init(0)
            .Where(Tag.TeamId(teamId))
            .When<KeyAdded>((_, count) => count + 1);
}
