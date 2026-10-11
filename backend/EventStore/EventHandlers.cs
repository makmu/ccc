namespace CCC.EventStore;

public sealed class EventHandlers<TResult>
{
    private readonly Dictionary<string, Func<EventRecord, TResult>> _handlers = [];

    public IReadOnlyCollection<string> EventTypes => _handlers.Keys;

    public EventHandlers<TResult> On<TEvent>(Func<TEvent, TResult> handler)
    {
        _handlers.Add(typeof(TEvent).Name, record => handler(record.PayloadAs<TEvent>()));
        return this;
    }

    public TResult Handle(EventRecord record) =>
        _handlers.TryGetValue(record.Type, out var handler)
            ? handler(record)
            : throw new InvalidOperationException($"No handler is registered for {record.Type}.");
}
