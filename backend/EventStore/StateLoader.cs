namespace CCC.EventStore;

public class StateLoader(IEventStore eventStore) : IStateLoader
{
    // One query for all models, filtered by the union of their filters, so every
    // model is folded from the same snapshot of the event log — loading them one by
    // one would let a concurrent append land between two queries. The price is
    // in-memory tag re-matching: a record only matching model A still comes back
    // when model B's entries are part of the same union.
    public async Task<StateContext> LoadAsync(IReadOnlyList<IStateModel> models)
    {
        var filters = models.Select(model => model.Filter).ToArray();
        var union = new EventFilter(filters.SelectMany(filter => filter.Entries).ToList());
        var maxPosition = -1L;

        if (union.Entries.Count > 0)
        {
            foreach (var record in await eventStore.LoadAsync(union))
            {
                for (var i = 0; i < models.Count; i++)
                {
                    if (filters[i].Matches(record))
                        models[i].Apply(record);
                }

                if (record.Position > maxPosition)
                    maxPosition = record.Position;
            }
        }

        return new StateContext(eventStore, union, maxPosition);
    }
}
