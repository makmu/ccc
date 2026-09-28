using System.Text.Json;
using CCC.EventStore;

namespace CCC.Commands.Tests;

// Mirrors PostgresEventStore's semantics without a database: a filter matches
// the way EventFilter.Matches does, and a conditional append fails when an
// event matching the filter has landed after the expected position.
sealed class InMemoryEventStore : IEventStore
{
    private readonly List<EventRecord> _events = [];

    public IReadOnlyList<EventRecord> Events => _events;

    // Runs right before a conditional append checks its condition, so a test
    // can slip in a concurrent write.
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

    // Appends an event with the given tags, bypassing every check.
    public void Append<TEvent>(TEvent @event, Tags tags) => Add(typeof(TEvent).Name, @event!, Guid.Empty, tags.ToArray());

    private void Add(string type, object payload, Guid user, string[] tags) =>
        // Serialized through its runtime type, as PostgresEventStore does.
        _events.Add(new EventRecord(_events.Count + 1, type, JsonSerializer.Serialize(payload), DateTimeOffset.UtcNow, user, tags));
}
