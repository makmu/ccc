namespace CCC.Domain;

static class Tag
{
    public static (string Key, string Value) TenantId(Guid id) => ("tenant-id", id.ToString());
    public static (string Key, string Value) TenantName(string name) => ("tenant-name", name);
    public static (string Key, string Value) TeamId(Guid id) => ("team-id", id.ToString());
    public static (string Key, string Value) TeamName(string name) => ("team-name", name);
    public static (string Key, string Value) ProjectId(Guid id) => ("project-id", id.ToString());
    public static (string Key, string Value) ProjectName(string name) => ("project-name", name);
    public static (string Key, string Value) KeyId(Guid id) => ("key-id", id.ToString());
    public static (string Key, string Value) KeyName(string name) => ("key-name", name);
    public static (string Key, string Value) LanguageCode(string code) => ("language-code", code);
    public static (string Key, string Value) UserId(Guid id) => ("user-id", id.ToString());
    public static (string Key, string Value) UserEmail(string email) => ("user-email", email);
    public static (string Key, string Value) SubscriptionId(Guid id) => ("subscription-id", id.ToString());
}
