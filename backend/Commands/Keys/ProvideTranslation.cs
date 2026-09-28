using CCC.Domain.Types;

namespace CCC.Commands.Keys;

public record ProvideTranslation(TeamId TeamId, ProjectId ProjectId, KeyId KeyId, LanguageCode LanguageCode, Text Text);
