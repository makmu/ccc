namespace CCC.Domain.Events;

public record LanguageAdded(string LanguageCode, string ParentLanguageCode, Guid ProjectId);
