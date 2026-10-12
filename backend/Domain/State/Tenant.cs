using CCC.Base.Dcb;
using CCC.Domain.Events;
using CCC.Domain.Types;

namespace CCC.Domain.State;

public static class Tenant
{
    public static StateModel<bool> Exists(TenantId tenantId) =>
        StateModel.Init(false)
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>((_, _) => true);

    public static StateModel<bool> NameTaken(TenantName name) =>
        StateModel.Init(false)
            .Where(Tag.TenantName(name))
            .When<TenantRegistered>((_, _) => true)
            .When<TenantRenamed>((e, _) => CanonicalForm.Of(e.Name) == name.Canonical);

    public static StateModel<TenantName?> Name(TenantId tenantId) =>
        StateModel.Init<TenantName?>(null)
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>((e, _) => TenantName.From(e.Name))
            .When<TenantRenamed>((e, _) => TenantName.From(e.Name));

    public static StateModel<bool> IsNamed(TenantId tenantId, TenantName name) =>
        StateModel.Init(false)
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>((e, _) => CanonicalForm.Of(e.Name) == name.Canonical)
            .When<TenantRenamed>((e, _) => CanonicalForm.Of(e.Name) == name.Canonical);

    public static StateModel<IReadOnlyList<SubscriptionModelId>> SubscriptionModels(TenantId tenantId) =>
        StateModel.Init<IReadOnlyList<SubscriptionModelId>>([])
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>((e, _) => e.SubscriptionModelIds.Select(SubscriptionModelId.From).ToList());

    public static StateModel<bool> HasTeams(TenantId tenantId) =>
        StateModel.Init(false)
            .Where(Tag.TenantId(tenantId))
            .When<TeamAdded>((_, _) => true);

    public static StateModel<bool> HasSubscription(TenantId tenantId, SubscriptionModelId subscriptionModelId) =>
        StateModel.Init(false)
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>((e, _) => e.SubscriptionModelIds.Contains(subscriptionModelId.Value));
}
