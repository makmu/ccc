using CCC.Domain.Events;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Domain.State;

public static class Project
{
    public static StateModel<bool> IdTaken(ProjectId projectId) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .When<ProjectAdded>((_, _) => true);

    public static StateModel<bool> NameTaken(TeamId teamId, ProjectName name) =>
        StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .Where(Tag.ProjectName(name))
            .When<ProjectAdded>((_, _) => true);

    public static StateModel<bool> InTeam(TeamId teamId, ProjectId projectId) =>
        StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .Where(Tag.ProjectId(projectId))
            .When<ProjectAdded>((_, _) => true);

    public static StateModel<IReadOnlyList<LanguageCode>> Languages(ProjectId projectId) =>
        StateModel.Init<IReadOnlyList<LanguageCode>>([])
            .Where(Tag.ProjectId(projectId))
            .When<ProjectAdded>((e, _) => [LanguageCode.From(e.ReferenceLanguage)])
            .When<LanguageAdded>((e, languages) => [.. languages, LanguageCode.From(e.LanguageCode)]);
}
