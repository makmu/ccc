namespace CCC.Infrastructure.EventStore;

class CommandContextBuilder(EventStore eventStore)
{
    private readonly EventFilterBuilder _filterBuilder = new();

    public CommandContextBuilder Where<T>()
    {
        _filterBuilder.Where<T>();
        return this;
    }

    public CommandContextBuilder Where<T>(Action<TypeFilterBuilder<T>> configure)
    {
        _filterBuilder.Where<T>(configure);
        return this;
    }

    public async Task<CommandContext> LoadAsync()
    {
        var filter = _filterBuilder.Build();
        var events = (await eventStore.LoadAsync(filter)).ToList();
        var maxPosition = events.Count != 0 ? events.Max(e => e.Position) : -1L;
        return new CommandContext(eventStore, filter, events, maxPosition);
    }
}
