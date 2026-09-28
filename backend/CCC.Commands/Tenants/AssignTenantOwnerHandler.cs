using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.EventStore;

namespace CCC.Commands.Tenants;

sealed class AssignTenantOwnerHandler(IStateLoader loader) : ICommandHandler<AssignTenantOwner>
{
    public async Task<CommandResult> HandleAsync(AssignTenantOwner command)
    {
        var tenantIdRegistered = Tenant.Exists(command.TenantId);
        var userAdded = User.Exists(command.UserId);
        var userAlreadyAssigned = User.IsTenantOwner(command.UserId, command.TenantId);

        var context = await loader.LoadAsync(tenantIdRegistered, userAdded, userAlreadyAssigned);

        if (!tenantIdRegistered)
            return CommandResult.NotFound("Tenant not found.");

        if (!userAdded)
            return CommandResult.NotFound("User not found.");

        if (userAlreadyAssigned)
            return CommandResult.Conflict("This user is already an owner of this tenant.");

        return await context.TryAppendAsync(
            new TenantOwnerAssigned(command.UserId.Value, command.TenantId.Value),
            Tags.From(Tag.TenantId(command.TenantId), Tag.UserId(command.UserId)));
    }
}
