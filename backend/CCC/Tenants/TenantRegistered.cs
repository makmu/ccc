namespace CCC.Tenants;

record TenantRegistered(Guid Id, string Name, IReadOnlyList<Guid> SubscriptionModelIds);
