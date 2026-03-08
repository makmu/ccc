namespace CCC.Infrastructure.EventStore;

class CommandContext(EventStore eventStore, EventFilter filter, IReadOnlyList<EventRecord> events, long maxPosition)
{
    public IReadOnlyList<EventRecord> Events => events;

    public Task AppendAsync<T>(T payload)
        => eventStore.AppendAsync(typeof(T).Name, payload!, Guid.Empty, maxPosition, filter);
}
