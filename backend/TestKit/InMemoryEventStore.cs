using System.Text.Json;
using CCC.EventStore;

namespace CCC.TestKit;

public sealed class InMemoryEventStore : IEventStore
{
    private readonly List<EventRecord> _events = [];

    public IReadOnlyList<EventRecord> Events => _events;

    public Action? BeforeConditionalAppend { get; set; }

    public Task AppendAsync(string type, object payload, Guid user)
    {
        Add(type, payload, user, []);
        return Task.CompletedTask;
    }

    public Task AppendAsync(string eventType, object eventPayload, Guid user, long expectedMaxPosition, EventFilter contextFilter, string[] tags)
    {
        BeforeConditionalAppend?.Invoke();

        var maxPosition = _events.Where(contextFilter.Matches).Select(e => e.Position).DefaultIfEmpty(-1).Max();
        if (maxPosition != expectedMaxPosition)
            throw new ConcurrencyException("The event store was modified by a concurrent operation.");

        Add(eventType, eventPayload, user, tags);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<EventRecord>> ReadAsync(long fromPosition, params string[] types) =>
        Task.FromResult<IEnumerable<EventRecord>>(
            _events.Where(e => e.Position >= fromPosition && types.Contains(e.Type)).ToList());

    public Task<IEnumerable<EventRecord>> LoadAsync(EventFilter filter) =>
        Task.FromResult<IEnumerable<EventRecord>>(_events.Where(filter.Matches).ToList());

    public void Append<TEvent>(TEvent @event, Tags tags) => Add(typeof(TEvent).Name, @event!, Guid.Empty, tags.ToArray());

    private void Add(string type, object payload, Guid user, string[] tags) =>
        _events.Add(new EventRecord(_events.Count + 1, type, JsonSerializer.Serialize(payload), DateTimeOffset.UtcNow, user, tags));
}
