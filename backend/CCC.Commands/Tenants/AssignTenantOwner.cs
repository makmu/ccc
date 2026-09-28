using CCC.Domain.Types;

namespace CCC.Commands.Tenants;

public record AssignTenantOwner(TenantId TenantId, UserId UserId);
