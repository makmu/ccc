namespace CCC.Organizations;

public record AddOrganizationRequest(Guid Id, string Name, IReadOnlyList<Guid> SubscriptionModelIds);
