namespace CCC.Infrastructure.EventStore;

record EventFilter(IReadOnlyList<EventFilter.Entry> Entries)
{
    internal record Entry(
        string Type,
        IReadOnlyList<string> Tags);

    // The union filter of all models is queried in one go, so a record coming back
    // is only known to match *some* model. This re-checks a single filter against a
    // record with the same semantics the SQL uses: any entry whose type matches and
    // whose tags are a subset of the record's tags (Postgres' tags @> entry.tags).
    public bool Matches(EventRecord record)
        => Entries.Any(entry =>
            entry.Type == record.Type &&
            entry.Tags.All(record.Tags.Contains));
}
