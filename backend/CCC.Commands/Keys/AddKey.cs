using CCC.Domain.ValueObjects;

namespace CCC.Commands.Keys;

public record AddKey(TeamId TeamId, ProjectId ProjectId, KeyId Id, KeyName Name);
