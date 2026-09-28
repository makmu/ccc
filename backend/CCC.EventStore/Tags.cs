using System.Text.RegularExpressions;

namespace CCC.EventStore;

// A set of "key=value" identity tags attached to an event, orthogonal to its
// payload and type. Matching is a subset check (a filter matches an event when
// every one of the filter's entries is present on the event). Entries are sorted
// on construction so equality and hashing are order-insensitive (set semantics).
public sealed partial class Tags : IEquatable<Tags>
{
    private readonly string[] _values; // sorted "key=value" entries

    private Tags(string[] values) => _values = values;

    public static Tags Empty { get; } = new([]);

    public IReadOnlyList<string> Values => _values;

    public static Tags From(params (string Key, string Value)[] tags)
    {
        var entries = new string[tags.Length];
        for (var i = 0; i < tags.Length; i++)
        {
            var (key, value) = tags[i];
            // Keys are code-defined identifiers (no whitespace); values are
            // caller data (e.g. a tenant name) and may contain spaces.
            if (!Valid().IsMatch(key))
                throw new ArgumentException($"Invalid tag key: \"{key}\"");
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Invalid tag value: \"{value}\" for key \"{key}\"");
            entries[i] = $"{key}={value}";
        }

        Array.Sort(entries, StringComparer.Ordinal);
        return new Tags(entries);
    }

    public string[] ToArray() => (string[])_values.Clone();

    public bool Equals(Tags? other) => other is not null && _values.SequenceEqual(other._values);
    public override bool Equals(object? o) => Equals(o as Tags);
    public override int GetHashCode() => _values.Aggregate(17, (h, v) => h * 31 + v.GetHashCode());

    [GeneratedRegex(@"^\S+$")]
    private static partial Regex Valid();
}
