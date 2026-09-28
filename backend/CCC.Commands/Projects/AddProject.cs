using CCC.Domain.ValueObjects;

namespace CCC.Commands.Projects;

public record AddProject(TeamId TeamId, ProjectId Id, ProjectName Name, LanguageCode ReferenceLanguage);
