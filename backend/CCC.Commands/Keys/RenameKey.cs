using CCC.Domain.ValueObjects;

namespace CCC.Commands.Keys;

public record RenameKey(TeamId TeamId, ProjectId ProjectId, KeyId KeyId, KeyName Name);
