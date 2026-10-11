using CCC.Base.Abstractions;
using CCC.Base.Dcb;
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
        var name = Tenant.Name(command.TenantId);
        var alreadyNamed = Tenant.IsNamed(command.TenantId, command.Name);
        var nameTaken = Tenant.NameTaken(command.Name);
        var context = await loader.LoadAsync([name, alreadyNamed, nameTaken], cancellationToken);

        if (name.IsNull(out var currentName))
            return CommandResult.NotFound("Tenant not found.");

        if (alreadyNamed.IsTrue())
            return CommandResult.Ok;

        if (nameTaken.IsTrue())
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
