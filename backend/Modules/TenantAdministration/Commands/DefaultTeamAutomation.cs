using CCC.Base.Abstractions;
using CCC.Base.Automations;
using CCC.Base.Dcb;
using CCC.Domain.Events;
using CCC.Domain.Types;

namespace CCC.Modules.TenantAdministration.Commands;

sealed class DefaultTeamAutomation(ICommandHandler<AddDefaultTeam> addDefaultTeam) : IAutomation
{
    public string Schema => "default_team_automation";

    public EventHandlers<Reaction> Handlers { get; } = new EventHandlers<Reaction>()
        .On<TenantRegistered>(e => cancellationToken =>
            addDefaultTeam.HandleAsync(new AddDefaultTeam(TenantId.From(e.Id), TeamId.From(Guid.NewGuid())), cancellationToken));
}
