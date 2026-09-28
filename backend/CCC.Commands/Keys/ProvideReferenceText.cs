using CCC.Domain.ValueObjects;

namespace CCC.Commands.Keys;

public record ProvideReferenceText(TeamId TeamId, ProjectId ProjectId, KeyId KeyId, string Text);
