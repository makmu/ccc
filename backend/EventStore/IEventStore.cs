using CCC.Base.Dcb;
namespace CCC.EventStore;

public interface IEventStore
{
    Task AppendAsync(string type, object payload, Guid user, CancellationToken cancellationToken);

    // Appends only if no event matching contextFilter has landed after
    // expectedMaxPosition; throws ConcurrencyException otherwise.
    Task AppendAsync(string eventType, object eventPayload, Guid user, long expectedMaxPosition, EventFilter contextFilter, string[] tags,
        CancellationToken cancellationToken);

    Task<IEnumerable<EventRecord>> ReadAsync(long fromPosition, int limit, string[] types, CancellationToken cancellationToken);

    Task<IEnumerable<EventRecord>> LoadAsync(EventFilter filter, CancellationToken cancellationToken);
}
