namespace CCC.Api.Tenants;

public record RegisterTenantRequest(Guid Id, string Name, IReadOnlyList<Guid> SubscriptionModelIds);
