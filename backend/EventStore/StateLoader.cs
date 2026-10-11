using CCC.Base.Dcb;
namespace CCC.EventStore;

public class StateLoader(IEventStore eventStore) : IStateLoader
{
    // One query for all models, filtered by the union of their filters, so every
    // model is folded from the same snapshot of the event log — loading them one by
    // one would let a concurrent append land between two queries. The price is
    // in-memory tag re-matching: a record only matching model A still comes back
    // when model B's entries are part of the same union.
    public async Task<StateContext> LoadAsync(IReadOnlyList<IStateModel> models, CancellationToken cancellationToken)
    {
        var union = new EventFilter(models.SelectMany(model => model.Filter.Entries).ToList());
        var records = union.Entries.Count > 0
            ? (await eventStore.LoadAsync(union, cancellationToken)).ToList()
            : [];

        foreach (var model in models)
            model.Load(records.Where(model.Filter.Matches));

        var maxPosition = records.Count > 0 ? records.Max(record => record.Position) : -1L;
        return new StateContext(eventStore, union, maxPosition);
    }
}
