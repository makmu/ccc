using System.Text.RegularExpressions;

namespace CCC.Base.Dcb;

public sealed partial class Tags : IEquatable<Tags>
{
    private readonly string[] _values;

    private Tags(string[] values) => _values = values;

    public static Tags Empty { get; } = new([]);

    public IReadOnlyList<string> Values => _values;

    public static Tags From(params (string Key, string Value)[] tags)
    {
        var entries = new string[tags.Length];
        for (var i = 0; i < tags.Length; i++)
        {
            var (key, value) = tags[i];
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
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var value in _values)
            hash.Add(value);
        return hash.ToHashCode();
    }

    [GeneratedRegex(@"^\S+$")]
    private static partial Regex Valid();
}
