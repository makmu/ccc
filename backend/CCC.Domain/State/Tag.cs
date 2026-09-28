using CCC.Domain.Types;

namespace CCC.Domain.State;

public static class Tag
{
    public static (string Key, string Value) TenantId(TenantId id) => ("tenant-id", id.Value.ToString());
    public static (string Key, string Value) TenantName(TenantName name) => ("tenant-name", name.Canonical);
    public static (string Key, string Value) TeamId(TeamId id) => ("team-id", id.Value.ToString());
    public static (string Key, string Value) TeamName(TeamName name) => ("team-name", name.Canonical);
    public static (string Key, string Value) ProjectId(ProjectId id) => ("project-id", id.Value.ToString());
    public static (string Key, string Value) ProjectName(ProjectName name) => ("project-name", name.Canonical);
    public static (string Key, string Value) KeyId(KeyId id) => ("key-id", id.Value.ToString());
    public static (string Key, string Value) KeyName(KeyName name) => ("key-name", name.Value);
    public static (string Key, string Value) LanguageCode(LanguageCode code) => ("language-code", code.Value);
    public static (string Key, string Value) UserId(UserId id) => ("user-id", id.Value.ToString());
    public static (string Key, string Value) UserEmail(EmailAddress email) => ("user-email", email.Value);
    public static (string Key, string Value) SubscriptionId(SubscriptionModelId id) => ("subscription-id", id.Value.ToString());
}
