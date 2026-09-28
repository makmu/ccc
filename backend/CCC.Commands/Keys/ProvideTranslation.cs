using CCC.Domain.ValueObjects;

namespace CCC.Commands.Keys;

public record ProvideTranslation(TeamId TeamId, ProjectId ProjectId, KeyId KeyId, LanguageCode LanguageCode, string Text);
