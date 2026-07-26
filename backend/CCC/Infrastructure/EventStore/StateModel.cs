using System.Text.Json;

namespace CCC.Infrastructure.EventStore;

// Non-generic factory so callers write StateModel.Init(false) and let the
// compiler infer the state type.
static class StateModel
{
    public static StateModel<T> Init<T>(T initialState) => new(initialState);
}

// A single decision/read model: an initial state folded forward over the events
// matching its tags and handled event types. State is exposed strongly typed;
// the fold happens through the erased IStateModel.Apply.
sealed class StateModel<T>(T initialState) : IStateModel
{
    private readonly List<(string Key, string Value)> _tags = [];
    private readonly Dictionary<string, Func<EventRecord, T, T>> _reducers = [];

    public T State { get; private set; } = initialState;

    public static implicit operator T(StateModel<T> model) => model.State;

    public StateModel<T> Where(string key, string value)
    {
        _tags.Add((key, value));
        return this;
    }

    // Tuple overload so a Tag factory result flows straight in:
    // .Where(Tag.TeamId(id)) instead of .Where(TagKeys.TeamId, id.ToString()).
    public StateModel<T> Where((string Key, string Value) tag)
    {
        _tags.Add(tag);
        return this;
    }

    public StateModel<T> When<TEvent>(Func<TEvent, T, T> reduce)
    {
        // Capture TEvent here so Apply can stay type-erased: it deserializes the
        // stored payload to the right event type before invoking the reducer.
        _reducers[typeof(TEvent).Name] =
            (record, state) =>
            {
                var deserializedEvent = JsonSerializer.Deserialize<TEvent>(record.Payload);
                if (deserializedEvent is null)
                    throw new InvalidOperationException($"Failed to deserialize event of type {typeof(TEvent).Name}");
                
                return reduce(deserializedEvent, state);
            };
        return this;
    }

    // One filter entry per handled event type, each carrying this model's tags.
    // The union of every model's entries becomes the append condition.
    public EventFilter Filter
    {
        get
        {
            var tags = Tags.From(_tags.ToArray()).Values;
            var entries = _reducers.Keys
                .Select(type => new EventFilter.Entry(type, tags))
                .ToList();
            return new EventFilter(entries);
        }
    }

    // Events are queried per model, so every record handed here already matches
    // this model's tags — only the event type still needs dispatching.
    public void Apply(EventRecord record)
    {
        if (_reducers.TryGetValue(record.Type, out var reduce))
            State = reduce(record, State);
    }
}
