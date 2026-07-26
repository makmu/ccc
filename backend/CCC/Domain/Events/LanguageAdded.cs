namespace CCC.Domain.Events;

record LanguageAdded(string LanguageCode, string ParentLanguageCode, Guid ProjectId);
