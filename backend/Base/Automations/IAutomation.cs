using CCC.Base.Abstractions;
using CCC.Base.Dcb;

namespace CCC.Base.Automations;

public delegate Task<CommandResult> Reaction(CancellationToken cancellationToken);

public interface IAutomation
{
    // Each automation owns one schema, which holds only its bookmark. Unlike a
    // projection, an automation has no version and is never replayed, since a
    // replay would issue its commands again.
    string Schema { get; }

    EventHandlers<Reaction> Handlers { get; }
}
