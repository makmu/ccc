using CCC.Domain.Events;
using CCC.Domain.ValueObjects;
using CCC.EventStore;

namespace CCC.Domain.State;

public static class State
{
    public static StateModel<bool> TenantExists(TenantId tenantId) =>
        StateModel.Init(false)
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>((_, _) => true);

    public static StateModel<bool> TenantNameTaken(TenantName name) =>
        StateModel.Init(false)
            .Where(Tag.TenantName(name))
            .When<TenantRegistered>((_, _) => true)
            .When<TenantRenamed>((e, _) => CanonicalForm.Of(e.Name) == name.Canonical);

    public static StateModel<Guid[]> TenantSubscriptions(TenantId tenantId) =>
        StateModel.Init(Array.Empty<Guid>())
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>((e, _) => e.SubscriptionModelIds.ToArray());

    public static StateModel<string> CurrentTenantName(TenantId tenantId) =>
        StateModel.Init(string.Empty)
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>((e, _) => e.Name)
            .When<TenantRenamed>((e, _) => e.Name);

    public static StateModel<bool> TeamIdTaken(TenantId tenantId, TeamId teamId) =>
        StateModel.Init(false)
            .Where(Tag.TenantId(tenantId))
            .Where(Tag.TeamId(teamId))
            .When<TeamAdded>((_, _) => true);

    public static StateModel<bool> TeamNameTaken(TenantId tenantId, TeamName name) =>
        StateModel.Init(false)
            .Where(Tag.TenantId(tenantId))
            .Where(Tag.TeamName(name))
            .When<TeamAdded>((_, _) => true);

    public static StateModel<bool> UserExists(UserId userId) =>
        StateModel.Init(false)
            .Where(Tag.UserId(userId))
            .When<UserAdded>((_, _) => true);

    public static StateModel<bool> UserEmailTaken(EmailAddress email) =>
        StateModel.Init(false)
            .Where(Tag.UserEmail(email))
            .When<UserAdded>((_, _) => true);

    public static StateModel<bool> UserIsTenantOwner(UserId userId, TenantId tenantId) =>
        StateModel.Init(false)
            .Where(Tag.UserId(userId))
            .Where(Tag.TenantId(tenantId))
            .When<TenantOwnerAssigned>((_, _) => true);

    public static StateModel<bool> SubscriptionExists(SubscriptionModelId subscriptionId) =>
        StateModel.Init(false)
            .Where(Tag.SubscriptionId(subscriptionId))
            .When<SubscriptionModelAdded>((_, _) => true);

    public static StateModel<int> KeyLimit(SubscriptionModelId subscriptionId) =>
        StateModel.Init(0)
            .Where(Tag.SubscriptionId(subscriptionId))
            .When<SubscriptionModelAdded>((e, _) => e.KeyLimit);

    public static StateModel<bool> TeamExists(TeamId teamId) =>
        StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .When<TeamAdded>((_, _) => true);

    public static StateModel<Guid?> TeamSubscription(TeamId teamId) =>
        StateModel.Init<Guid?>(null)
            .Where(Tag.TeamId(teamId))
            .When<TeamAdded>((e, _) => e.SubscriptionModelId);

    public static StateModel<bool> ProjectIdTaken(ProjectId projectId) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .When<ProjectAdded>((_, _) => true);

    public static StateModel<bool> ProjectNameTaken(TeamId teamId, ProjectName name) =>
        StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .Where(Tag.ProjectName(name))
            .When<ProjectAdded>((_, _) => true);

    public static StateModel<bool> ProjectInTeam(TeamId teamId, ProjectId projectId) =>
        StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .Where(Tag.ProjectId(projectId))
            .When<ProjectAdded>((_, _) => true);

    public static StateModel<bool> LanguageInProject(ProjectId projectId, LanguageCode languageCode) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.LanguageCode(languageCode))
            .When<LanguageAdded>((_, _) => true);

    public static StateModel<bool> ValidParentLanguage(ProjectId projectId, LanguageCode parentLanguageCode) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.LanguageCode(parentLanguageCode))
            .When<ProjectAdded>((_, _) => true)
            .When<LanguageAdded>((e, _) => e.LanguageCode == parentLanguageCode.Value);

    public static StateModel<bool> IsReferenceLanguage(ProjectId projectId, LanguageCode languageCode) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.LanguageCode(languageCode))
            .When<ProjectAdded>((_, _) => true);

    public static StateModel<bool> KeyExists(TeamId teamId, ProjectId projectId, KeyId keyId) =>
        StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.KeyId(keyId))
            .When<KeyAdded>((_, _) => true);

    public static StateModel<bool> KeyNameTaken(ProjectId projectId, KeyName name) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.KeyName(name))
            .When<KeyAdded>((_, _) => true)
            .When<KeyRenamed>((e, _) => e.Name == name.Value);

    public static StateModel<int> TeamKeyCount(TeamId teamId) =>
        StateModel.Init(0)
            .Where(Tag.TeamId(teamId))
            .When<KeyAdded>((_, count) => count + 1);

    public static StateModel<string> CurrentReferenceText(KeyId keyId) =>
        StateModel.Init(string.Empty)
            .Where(Tag.KeyId(keyId))
            .When<ReferenceTextProvided>((e, _) => e.Text);

    public static StateModel<string> CurrentTranslation(KeyId keyId, LanguageCode languageCode) =>
        StateModel.Init(string.Empty)
            .Where(Tag.KeyId(keyId))
            .Where(Tag.LanguageCode(languageCode))
            .When<TranslationProvided>((e, _) => e.Text);

    public static StateModel<string> CurrentKeyName(KeyId keyId) =>
        StateModel.Init(string.Empty)
            .Where(Tag.KeyId(keyId))
            .When<KeyAdded>((e, _) => e.Name)
            .When<KeyRenamed>((e, _) => e.Name);
}
