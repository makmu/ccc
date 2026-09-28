namespace CCC.Domain.Events;

public record TranslationProvided(Guid KeyId, Guid ProjectId, string LanguageCode, string Text);
