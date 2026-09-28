namespace CCC.Domain.Events;

public record KeyAdded(Guid Id, string Name, Guid ProjectId);
