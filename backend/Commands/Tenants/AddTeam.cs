using CCC.Domain.Types;

namespace CCC.Commands.Tenants;

public record AddTeam(TenantId TenantId, TeamId Id, TeamName Name, SubscriptionModelId SubscriptionModelId);
