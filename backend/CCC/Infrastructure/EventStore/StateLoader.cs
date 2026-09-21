namespace CCC.Infrastructure.EventStore;

class StateLoader(EventStore eventStore)
{
    // One query per model (each already narrowed to its own types + tags, so no
    // in-memory tag re-matching is needed). The append condition is the union of
    // every model's filter, guarded by the highest position seen across them.
    public async Task<StateContext> LoadAsync(params IStateModel[] models)
    {
        var entries = new List<EventFilter.Entry>();
        var maxPosition = -1L;

        foreach (var model in models)
        {
            var filter = model.Filter;
            entries.AddRange(filter.Entries);

            foreach (var record in await eventStore.LoadAsync(filter)) // ordered by position
            {
                model.Apply(record);
                if (record.Position > maxPosition)
                    maxPosition = record.Position;
            }
        }

        return new StateContext(eventStore, new EventFilter(entries), maxPosition);
    }
}
