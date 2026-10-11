using CCC.Domain.Events;
using CCC.Domain.Types;
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

    public static StateModel<bool> IdTaken(KeyId keyId) =>
        StateModel.Init(false)
            .Where(Tag.KeyId(keyId))
            .When<KeyAdded>((_, _) => true);

    public static StateModel<bool> NameTaken(ProjectId projectId, KeyName name) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.KeyName(name))
            .When<KeyAdded>((_, _) => true)
            .When<KeyRenamed>((e, _) => e.Name == name.Value);

    public static StateModel<KeyName?> Name(KeyId keyId) =>
        StateModel.Init<KeyName?>(null)
            .Where(Tag.KeyId(keyId))
            .When<KeyAdded>((e, _) => KeyName.From(e.Name))
            .When<KeyRenamed>((e, _) => KeyName.From(e.Name));

    public static StateModel<bool> IsNamed(KeyId keyId, KeyName name) =>
        StateModel.Init(false)
            .Where(Tag.KeyId(keyId))
            .When<KeyAdded>((e, _) => e.Name == name.Value)
            .When<KeyRenamed>((e, _) => e.Name == name.Value);

    // A key without a reference text or translation counts as having an empty one.
    public static StateModel<bool> HasReferenceText(KeyId keyId, Text text) =>
        StateModel.Init(text.Value == string.Empty)
            .Where(Tag.KeyId(keyId))
            .When<ReferenceTextProvided>((e, _) => e.Text == text.Value);

    public static StateModel<bool> HasTranslation(KeyId keyId, LanguageCode languageCode, Text text) =>
        StateModel.Init(text.Value == string.Empty)
            .Where(Tag.KeyId(keyId))
            .Where(Tag.LanguageCode(languageCode))
            .When<TranslationProvided>((e, _) => e.Text == text.Value);
}
