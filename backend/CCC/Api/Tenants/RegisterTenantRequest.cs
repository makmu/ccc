namespace CCC.Tenants;

public record RegisterTenantRequest(Guid Id, string Name, IReadOnlyList<Guid> SubscriptionModelIds);
