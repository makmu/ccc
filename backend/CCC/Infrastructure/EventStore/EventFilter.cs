namespace CCC.Infrastructure.EventStore;

record EventFilter(IReadOnlyList<EventFilter.Entry> Entries)
{
    internal record Entry(
        string Type,
        IReadOnlyList<string> Tags);
}
