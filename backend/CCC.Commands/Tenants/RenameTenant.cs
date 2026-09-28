using CCC.Domain.ValueObjects;

namespace CCC.Commands.Tenants;

public record RenameTenant(TenantId TenantId, TenantName Name);
