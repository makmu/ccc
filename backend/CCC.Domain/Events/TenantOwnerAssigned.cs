namespace CCC.Domain.Events;

public record TenantOwnerAssigned(Guid UserId, Guid TenantId);
