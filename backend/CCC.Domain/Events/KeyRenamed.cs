namespace CCC.Domain.Events;

public record KeyRenamed(Guid KeyId, Guid ProjectId, string Name);
