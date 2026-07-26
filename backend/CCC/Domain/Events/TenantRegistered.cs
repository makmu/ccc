namespace CCC.Domain.Events;

record TenantRegistered(Guid Id, string Name, IReadOnlyList<Guid> SubscriptionModelIds);
