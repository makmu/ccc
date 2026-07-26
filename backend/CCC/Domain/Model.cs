using CCC.Domain.Events;
using CCC.Helpers;
using CCC.Infrastructure.EventStore;

namespace CCC.Domain;

static class Model
{
    public static StateModel<bool> TenantExists(Guid tenantId) =>
        StateModel.Init(false)
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>((_, _) => true);

    public static StateModel<bool> TenantNameTaken(string name)
    {
        var canonical = name.CanonicalName();
        return StateModel.Init(false)
            .Where(Tag.TenantName(canonical))
            .When<TenantRegistered>((_, _) => true)
            .When<TenantRenamed>((e, _) => e.Name.CanonicalName() == canonical);
    }

    public static StateModel<Guid[]> TenantSubscriptions(Guid tenantId) =>
        StateModel.Init(Array.Empty<Guid>())
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>((e, _) => e.SubscriptionModelIds.ToArray());

    public static StateModel<string> CurrentTenantName(Guid tenantId) =>
        StateModel.Init(string.Empty)
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>((e, _) => e.Name)
            .When<TenantRenamed>((e, _) => e.Name);

    public static StateModel<bool> TeamIdTaken(Guid tenantId, Guid teamId) =>
        StateModel.Init(false)
            .Where(Tag.TenantId(tenantId))
            .Where(Tag.TeamId(teamId))
            .When<TeamAdded>((_, _) => true);

    public static StateModel<bool> TeamNameTaken(Guid tenantId, string name)
    {
        var canonical = name.CanonicalName();
        return StateModel.Init(false)
            .Where(Tag.TenantId(tenantId))
            .Where(Tag.TeamName(canonical))
            .When<TeamAdded>((_, _) => true);
    }

    public static StateModel<bool> UserExists(Guid userId) =>
        StateModel.Init(false)
            .Where(Tag.UserId(userId))
            .When<UserAdded>((_, _) => true);

    public static StateModel<bool> UserEmailTaken(string email) =>
        StateModel.Init(false)
            .Where(Tag.UserEmail(email))
            .When<UserAdded>((_, _) => true);

    public static StateModel<bool> UserIsTenantOwner(Guid userId, Guid tenantId) =>
        StateModel.Init(false)
            .Where(Tag.UserId(userId))
            .Where(Tag.TenantId(tenantId))
            .When<TenantOwnerAssigned>((_, _) => true);

    public static StateModel<bool> SubscriptionExists(Guid subscriptionId) =>
        StateModel.Init(false)
            .Where(Tag.SubscriptionId(subscriptionId))
            .When<SubscriptionModelAdded>((_, _) => true);

    public static StateModel<int> KeyLimit(Guid subscriptionId) =>
        StateModel.Init(0)
            .Where(Tag.SubscriptionId(subscriptionId))
            .When<SubscriptionModelAdded>((e, _) => e.KeyLimit);

    public static StateModel<bool> TeamExists(Guid teamId) =>
        StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .When<TeamAdded>((_, _) => true);

    public static StateModel<Guid?> TeamSubscription(Guid teamId) =>
        StateModel.Init<Guid?>(null)
            .Where(Tag.TeamId(teamId))
            .When<TeamAdded>((e, _) => e.SubscriptionModelId);

    public static StateModel<bool> ProjectIdTaken(Guid projectId) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .When<ProjectAdded>((_, _) => true);

    public static StateModel<bool> ProjectNameTaken(Guid teamId, string name)
    {
        var canonical = name.CanonicalName();
        return StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .Where(Tag.ProjectName(canonical))
            .When<ProjectAdded>((_, _) => true);
    }

    public static StateModel<bool> ProjectInTeam(Guid teamId, Guid projectId) =>
        StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .Where(Tag.ProjectId(projectId))
            .When<ProjectAdded>((_, _) => true);

    public static StateModel<bool> LanguageInProject(Guid projectId, string languageCode) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.LanguageCode(languageCode))
            .When<LanguageAdded>((_, _) => true);

    public static StateModel<bool> ValidParentLanguage(Guid projectId, string parentLanguageCode) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.LanguageCode(parentLanguageCode))
            .When<ProjectAdded>((_, _) => true)
            .When<LanguageAdded>((e, _) => e.LanguageCode == parentLanguageCode);

    public static StateModel<bool> IsReferenceLanguage(Guid projectId, string languageCode) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.LanguageCode(languageCode))
            .When<ProjectAdded>((_, _) => true);

    public static StateModel<bool> KeyExists(Guid teamId, Guid projectId, Guid keyId) =>
        StateModel.Init(false)
            .Where(Tag.TeamId(teamId))
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.KeyId(keyId))
            .When<KeyAdded>((_, _) => true);

    public static StateModel<bool> KeyNameTaken(Guid projectId, string name) =>
        StateModel.Init(false)
            .Where(Tag.ProjectId(projectId))
            .Where(Tag.KeyName(name))
            .When<KeyAdded>((_, _) => true)
            .When<KeyRenamed>((e, _) => e.Name == name);

    public static StateModel<int> TeamKeyCount(Guid teamId) =>
        StateModel.Init(0)
            .Where(Tag.TeamId(teamId))
            .When<KeyAdded>((_, count) => count + 1);

    public static StateModel<string> CurrentReferenceText(Guid keyId) =>
        StateModel.Init(string.Empty)
            .Where(Tag.KeyId(keyId))
            .When<ReferenceTextProvided>((e, _) => e.Text);

    public static StateModel<string> CurrentTranslation(Guid keyId, string languageCode) =>
        StateModel.Init(string.Empty)
            .Where(Tag.KeyId(keyId))
            .Where(Tag.LanguageCode(languageCode))
            .When<TranslationProvided>((e, _) => e.Text);

    public static StateModel<string> CurrentKeyName(Guid keyId) =>
        StateModel.Init(string.Empty)
            .Where(Tag.KeyId(keyId))
            .When<KeyAdded>((e, _) => e.Name)
            .When<KeyRenamed>((e, _) => e.Name);
}
