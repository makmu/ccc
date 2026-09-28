using CCC.Domain.ValueObjects;

namespace CCC.Commands.Tenants;

public record RegisterTenant(TenantId Id, TenantName Name, IReadOnlyList<SubscriptionModelId> SubscriptionModelIds);
