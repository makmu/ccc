namespace CCC.Domain.Events;

record ProjectAdded(Guid Id, string Name, Guid TeamId, string ReferenceLanguage);
