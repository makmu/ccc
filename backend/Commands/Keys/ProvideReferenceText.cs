using CCC.Domain.Types;

namespace CCC.Commands.Keys;

public record ProvideReferenceText(TeamId TeamId, ProjectId ProjectId, KeyId KeyId, Text Text);
