namespace CCC.EventStore;

public class StateContext(IEventStore eventStore, EventFilter filter, long maxPosition)
{
    public Task AppendAsync<T>(T payload, Tags tags)
        => eventStore.AppendAsync(typeof(T).Name, payload!, Guid.Empty, maxPosition, filter, tags.ToArray());
}
