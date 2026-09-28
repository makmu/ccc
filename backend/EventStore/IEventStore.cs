namespace CCC.EventStore;

public interface IEventStore
{
    Task AppendAsync(string type, object payload, Guid user);

    // Appends only if no event matching contextFilter has landed after
    // expectedMaxPosition; throws ConcurrencyException otherwise.
    Task AppendAsync(string eventType, object eventPayload, Guid user, long expectedMaxPosition, EventFilter contextFilter, string[] tags);

    Task<IEnumerable<EventRecord>> ReadAsync(long fromPosition, params string[] types);

    Task<IEnumerable<EventRecord>> LoadAsync(EventFilter filter);
}
