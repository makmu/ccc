namespace CCC.Base.Dcb;

public static class StateModel
{
    public static StateModel<T> Init<T>(T initialState) => new(initialState);

    public static IStateModel<bool> All(IEnumerable<IStateModel<bool>> models) => new AllStateModel(models.ToArray());

    internal static InvalidOperationException NotLoaded(EventFilter filter)
    {
        var types = string.Join(", ", filter.Entries.Select(entry => entry.Type).Distinct());
        var tags = string.Join(", ", filter.Entries.SelectMany(entry => entry.Tags).Distinct());
        return new InvalidOperationException($"The state model of {types} tagged {tags} was read before it was loaded.");
    }
}

public sealed class StateModel<T>(T initialState) : IStateModel<T>
{
    private readonly List<(string Key, string Value)> _tags = [];
    private readonly EventHandlers<Func<T, T>> _reducers = new();
    private T? _state;
    private bool _loaded;

    public T Value => _loaded ? _state! : throw StateModel.NotLoaded(Filter);

    public StateModel<T> Where(string key, string value)
    {
        _tags.Add((key, value));
        return this;
    }

    public StateModel<T> Where((string Key, string Value) tag)
    {
        _tags.Add(tag);
        return this;
    }

    public StateModel<T> When<TEvent>(Func<TEvent, T, T> reduce)
    {
        _reducers.On<TEvent>(e => state => reduce(e, state));
        return this;
    }

    public EventFilter Filter
    {
        get
        {
            var tags = Tags.From(_tags.ToArray()).Values;
            var entries = _reducers.EventTypes
                .Select(type => new EventFilter.Entry(type, tags))
                .ToList();
            return new EventFilter(entries);
        }
    }

    // The loader has already matched this model's tags against the records, so
    // only the event type still needs dispatching.
    public void Load(IEnumerable<EventRecord> records)
    {
        _state = records.Aggregate(initialState, (state, record) => _reducers.Handle(record)(state));
        _loaded = true;
    }
}
