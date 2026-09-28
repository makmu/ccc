namespace CCC.Domain.Events;

public record TenantRenamed(Guid TenantId, string Name);
