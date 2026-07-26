namespace CCC.Domain.Events;

record TeamAdded(Guid Id, string Name, Guid TenantId, Guid SubscriptionModelId);
