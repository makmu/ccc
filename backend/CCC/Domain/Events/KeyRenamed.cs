namespace CCC.Domain.Events;

record KeyRenamed(Guid KeyId, Guid ProjectId, string Name);
