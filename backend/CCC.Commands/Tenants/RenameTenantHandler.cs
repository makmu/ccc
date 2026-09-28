using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.ValueObjects;
using CCC.EventStore;

namespace CCC.Commands.Tenants;

sealed class RenameTenantHandler(IStateLoader loader) : ICommandHandler<RenameTenant>
{
    public async Task<CommandResult> HandleAsync(RenameTenant command)
    {
        var tenantIdRegistered = State.TenantExists(command.TenantId);
        var currentTenantName = State.CurrentTenantName(command.TenantId);
        var tenantNameTaken = State.TenantNameTaken(command.Name);

        var context = await loader.LoadAsync(tenantIdRegistered, currentTenantName, tenantNameTaken);

        if (!tenantIdRegistered)
            return CommandResult.NotFound("Tenant not found.");

        // Case-insensitive no-op: renaming to the same name in any casing changes nothing.
        var currentName = TenantName.Create(currentTenantName.State);
        if (currentName.Canonical == command.Name.Canonical)
            return CommandResult.Ok;

        if (tenantNameTaken)
            return CommandResult.Conflict("A tenant with this name already exists.");

        // Tag both names under the same key, so later name lookups match this
        // rename whether it took a name or freed one.
        return await context.TryAppendAsync(
            new TenantRenamed(command.TenantId.Value, command.Name.Value),
            Tags.From(
                Tag.TenantId(command.TenantId),
                Tag.TenantName(currentName),
                Tag.TenantName(command.Name)));
    }
}
