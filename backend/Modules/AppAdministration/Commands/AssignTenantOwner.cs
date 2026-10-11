using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.AppAdministration.Commands;

public record AssignTenantOwner(TenantId TenantId, UserId UserId);

sealed class AssignTenantOwnerHandler(IStateLoader loader) : ICommandHandler<AssignTenantOwner>
{
    public async Task<CommandResult> HandleAsync(AssignTenantOwner command, CancellationToken cancellationToken)
    {
        var tenantExists = Tenant.Exists(command.TenantId);
        var userExists = User.Exists(command.UserId);
        var alreadyOwner = User.IsTenantOwner(command.UserId, command.TenantId);
        var context = await loader.LoadAsync([tenantExists, userExists, alreadyOwner], cancellationToken);

        if (tenantExists.IsFalse())
            return CommandResult.NotFound("Tenant not found.");

        if (userExists.IsFalse())
            return CommandResult.NotFound("User not found.");

        if (alreadyOwner.IsTrue())
            return CommandResult.Conflict("This user is already an owner of this tenant.");

        var appended = await context.TryAppendAsync(
            new TenantOwnerAssigned(command.UserId.Value, command.TenantId.Value),
            Tags.From(Tag.TenantId(command.TenantId), Tag.UserId(command.UserId)),
            cancellationToken);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
