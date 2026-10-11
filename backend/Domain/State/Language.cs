using CCC.Dcb;
using CCC.Domain.Events;
using CCC.Domain.Types;

namespace CCC.Domain.State;

public static class Language
{
    public static StateModel<bool> InProject(ProjectId projectId, LanguageCode languageCode) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.LanguageCode(languageCode))
            .When<LanguageAdded>((_, _) => true);

    public static StateModel<bool> ValidParent(ProjectId projectId, LanguageCode parentLanguageCode) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.LanguageCode(parentLanguageCode))
            .When<ProjectAdded>((_, _) => true)
            .When<LanguageAdded>((e, _) => e.LanguageCode == parentLanguageCode.Value);

    public static StateModel<bool> IsReference(ProjectId projectId, LanguageCode languageCode) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.LanguageCode(languageCode))
            .When<ProjectAdded>((_, _) => true);
}
