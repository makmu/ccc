namespace CCC.Domain.Events;

public record TenantRegistered(Guid Id, string Name, IReadOnlyList<Guid> SubscriptionModelIds);
