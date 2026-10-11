
namespace CCC.Base.Dcb;

public static class StateModel
{
    public static StateModel<T> Init<T>(T initialState) => new(initialState);

    public static IStateModel<bool> All(IEnumerable<IStateModel<bool>> models) => new AllStateModel(models.ToArray());
}

public sealed class StateModel<T>(T initialState) : IStateModel<T>
{
    private readonly List<(string Key, string Value)> _tags = [];
    private readonly EventHandlers<Func<T, T>> _reducers = new();

    public T State { get; private set; } = initialState;

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

    // The loader has already matched this model's tags against the record, so
    // only the event type still needs dispatching.
    public void Apply(EventRecord record) => State = _reducers.Handle(record)(State);
}
