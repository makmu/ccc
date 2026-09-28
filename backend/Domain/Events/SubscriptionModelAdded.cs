namespace CCC.Domain.Events;

public record SubscriptionModelAdded(Guid Id, string Name, int KeyLimit);
