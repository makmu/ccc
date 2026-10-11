using CCC.Abstractions;
using CCC.Dcb;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TenantAdministration.Commands;

public record RenameTenant(TenantId TenantId, TenantName Name);

sealed class RenameTenantHandler(IStateLoader loader) : ICommandHandler<RenameTenant>
{
    public async Task<CommandResult> HandleAsync(RenameTenant command, CancellationToken cancellationToken)
    {
        var (name, alreadyNamed, nameTaken, context) = await loader.LoadAsync(
            Tenant.Name(command.TenantId),
            Tenant.IsNamed(command.TenantId, command.Name),
            Tenant.NameTaken(command.Name),
            cancellationToken);

        if (name is not { } currentName)
            return CommandResult.NotFound("Tenant not found.");

        if (alreadyNamed)
            return CommandResult.Ok;

        if (nameTaken)
            return CommandResult.Conflict("A tenant with this name already exists.");

        // Tag both names under the same key, so later name lookups match this
        // rename whether it took a name or freed one.
        var appended = await context.TryAppendAsync(
            new TenantRenamed(command.TenantId.Value, command.Name.Value),
            Tags.From(
                Tag.TenantId(command.TenantId),
                Tag.TenantName(currentName),
                Tag.TenantName(command.Name)),
                cancellationToken);
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
