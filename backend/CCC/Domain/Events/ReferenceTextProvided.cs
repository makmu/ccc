namespace CCC.Domain.Events;

record ReferenceTextProvided(Guid KeyId, Guid ProjectId, string Text);
