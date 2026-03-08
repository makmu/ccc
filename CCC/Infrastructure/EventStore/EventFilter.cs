namespace CCC.Infrastructure.EventStore;

record EventFilter(IReadOnlyList<EventFilter.Entry> Entries)
{
    public static EventFilterBuilder Builder => new();

    internal record Entry(string Type, IReadOnlyDictionary<string, string> PayloadProperties);
}
