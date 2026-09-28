namespace CCC.Domain.Events;

public record UserAdded(Guid Id, string Name, string Email);
