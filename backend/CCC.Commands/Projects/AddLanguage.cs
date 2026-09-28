using CCC.Domain.ValueObjects;

namespace CCC.Commands.Projects;

public record AddLanguage(TeamId TeamId, ProjectId ProjectId, LanguageCode LanguageCode, LanguageCode ParentLanguageCode);
