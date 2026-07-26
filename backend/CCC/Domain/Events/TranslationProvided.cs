namespace CCC.Domain.Events;

record TranslationProvided(Guid KeyId, Guid ProjectId, string LanguageCode, string Text);
