using CCC.Abstractions;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.AppAdministration;

public record AssignTenantOwner(TenantId TenantId, UserId UserId);

sealed class AssignTenantOwnerHandler(IStateLoader loader) : ICommandHandler<AssignTenantOwner>
{
    public async Task<CommandResult> HandleAsync(AssignTenantOwner command)
    {
        var (tenantExists, userExists, alreadyOwner, context) = await loader.LoadAsync(
            Tenant.Exists(command.TenantId),
            User.Exists(command.UserId),
            User.IsTenantOwner(command.UserId, command.TenantId));

        if (!tenantExists)
            return CommandResult.NotFound("Tenant not found.");

        if (!userExists)
            return CommandResult.NotFound("User not found.");

        if (alreadyOwner)
            return CommandResult.Conflict("This user is already an owner of this tenant.");

        var appended = await context.TryAppendAsync(
            new TenantOwnerAssigned(command.UserId.Value, command.TenantId.Value),
            Tags.From(Tag.TenantId(command.TenantId), Tag.UserId(command.UserId)));
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
