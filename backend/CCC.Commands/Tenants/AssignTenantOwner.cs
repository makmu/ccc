using CCC.Domain.ValueObjects;

namespace CCC.Commands.Tenants;

public record AssignTenantOwner(TenantId TenantId, UserId UserId);
