namespace CCC.Domain.Events;

public record ProjectAdded(Guid Id, string Name, Guid TeamId, string ReferenceLanguage);
