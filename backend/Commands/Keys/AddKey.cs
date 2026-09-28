using CCC.Domain.Types;

namespace CCC.Commands.Keys;

public record AddKey(TeamId TeamId, ProjectId ProjectId, KeyId Id, KeyName Name);
