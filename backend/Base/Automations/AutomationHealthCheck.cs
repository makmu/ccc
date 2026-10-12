using CCC.Base.ModuleHosting;
using CCC.EventStore;

namespace CCC.Base.Automations;

sealed class AutomationHealthCheck<TAutomation>(
    TAutomation automation,
    IEventStore eventStore,
    ModuleDatabase database) : BookmarkHealthCheck(eventStore, database)
    where TAutomation : IAutomation
{
    protected override string Schema => automation.Schema;

    protected override IReadOnlyCollection<string> EventTypes => automation.Handlers.EventTypes;

    protected override string NotInstalledDescription => "Not installed yet, so it reacts to nothing.";
}
