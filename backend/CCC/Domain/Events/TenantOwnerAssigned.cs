namespace CCC.Domain.Events;

record TenantOwnerAssigned(Guid UserId, Guid TenantId);
