namespace CCC.EventStore;

public class StateContext(IEventStore eventStore, EventFilter filter, long maxPosition)
{
    // T is stored as the event type, so the event must not be passed along as object.
    public async Task<bool> TryAppendAsync<T>(T payload, Tags tags, CancellationToken cancellationToken)
    {
        try
        {
            await eventStore.AppendAsync(typeof(T).Name, payload!, Guid.Empty, maxPosition, filter, tags.ToArray(), cancellationToken);
            return true;
        }
        catch (ConcurrencyException)
        {
            return false;
        }
    }
}
