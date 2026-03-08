namespace CCC.Infrastructure.EventStore;

class EventFilterBuilder
{
    private readonly List<EventFilter.Entry> _entries = new();

    public EventFilterBuilder Where<T>()
    {
        _entries.Add(new EventFilter.Entry(typeof(T).Name, new Dictionary<string, string>()));
        return this;
    }

    public EventFilterBuilder Where<T>(Action<TypeFilterBuilder<T>> configure)
    {
        var builder = new TypeFilterBuilder<T>();
        configure(builder);
        _entries.Add(new EventFilter.Entry(typeof(T).Name, builder.Properties));
        return this;
    }

    public EventFilter Build() => new(_entries);
}
