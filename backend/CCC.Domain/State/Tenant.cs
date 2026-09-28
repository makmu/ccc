using CCC.Domain.Events;
using CCC.Domain.ValueObjects;
using CCC.EventStore;

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

    public static StateModel<Guid[]> Subscriptions(TenantId tenantId) =>
        StateModel.Init(Array.Empty<Guid>())
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>((e, _) => e.SubscriptionModelIds.ToArray());

    public static StateModel<string> Name(TenantId tenantId) =>
        StateModel.Init(string.Empty)
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>((e, _) => e.Name)
            .When<TenantRenamed>((e, _) => e.Name);
}
