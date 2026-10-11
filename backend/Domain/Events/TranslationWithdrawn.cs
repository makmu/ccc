namespace CCC.Domain.Events;

public record TranslationWithdrawn(Guid KeyId, Guid ProjectId, string LanguageCode);
