namespace CCC.Organizations;

record OrganizationAdded(Guid Id, string Name, IReadOnlyList<Guid> SubscriptionModelIds);
