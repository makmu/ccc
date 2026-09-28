using CCC.Domain.Events;
using CCC.Domain.ValueObjects;
using CCC.EventStore;

namespace CCC.Domain.State;

public static class User
{
    public static StateModel<bool> Exists(UserId userId) =>
        StateModel.Init(false)
            .Where(Tag.UserId(userId))
            .When<UserAdded>((_, _) => true);

    public static StateModel<bool> EmailTaken(EmailAddress email) =>
        StateModel.Init(false)
            .Where(Tag.UserEmail(email))
            .When<UserAdded>((_, _) => true);

    public static StateModel<bool> IsTenantOwner(UserId userId, TenantId tenantId) =>
        StateModel.Init(false)
            .Where(Tag.UserId(userId))
            .Where(Tag.TenantId(tenantId))
            .When<TenantOwnerAssigned>((_, _) => true);
}
