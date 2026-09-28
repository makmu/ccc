namespace CCC.Domain.Events;

public record TeamAdded(Guid Id, string Name, Guid TenantId, Guid SubscriptionModelId);
