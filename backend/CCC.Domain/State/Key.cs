using CCC.Domain.Events;
using CCC.Domain.ValueObjects;
using CCC.EventStore;

namespace CCC.Domain.State;

public static class Key
{
    public static StateModel<bool> Exists(TeamId teamId, ProjectId projectId, KeyId keyId) =>
        StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.KeyId(keyId))
            .When<KeyAdded>((_, _) => true);

    public static StateModel<bool> NameTaken(ProjectId projectId, KeyName name) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.KeyName(name))
            .When<KeyAdded>((_, _) => true)
            .When<KeyRenamed>((e, _) => e.Name == name.Value);

    public static StateModel<string> Name(KeyId keyId) =>
        StateModel.Init(string.Empty)
            .Where(Tag.KeyId(keyId))
            .When<KeyAdded>((e, _) => e.Name)
            .When<KeyRenamed>((e, _) => e.Name);

    public static StateModel<string> ReferenceText(KeyId keyId) =>
        StateModel.Init(string.Empty)
            .Where(Tag.KeyId(keyId))
            .When<ReferenceTextProvided>((e, _) => e.Text);

    public static StateModel<string> Translation(KeyId keyId, LanguageCode languageCode) =>
        StateModel.Init(string.Empty)
            .Where(Tag.KeyId(keyId))
            .Where(Tag.LanguageCode(languageCode))
            .When<TranslationProvided>((e, _) => e.Text);
}
