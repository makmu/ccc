using CCC.Domain.Types;

namespace CCC.Commands.Tenants;

public record RenameTenant(TenantId TenantId, TenantName Name);
