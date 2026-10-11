namespace CCC.Base.Dcb;

public record EventFilter(IReadOnlyList<EventFilter.Entry> Entries)
{
    public record Entry(
        string Type,
        IReadOnlyList<string> Tags);

    // StateLoader queries the union of all models' filters at once, so a record is
    // only known to match some model. This re-checks one filter with the SQL's
    // semantics: an entry's type matches and its tags are a subset of the record's.
    public bool Matches(EventRecord record)
        => Entries.Any(entry =>
            entry.Type == record.Type &&
            entry.Tags.All(record.Tags.Contains));
}
