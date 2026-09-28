namespace CCC.Domain.Events;

public record ReferenceTextProvided(Guid KeyId, Guid ProjectId, string Text);
