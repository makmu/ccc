namespace CCC.Domain.ValueObjects;

// Tagged exactly as entered. Unlike tenant, team and project names, key names
// are case-sensitive.
public readonly record struct KeyName
{
    public string Value { get; }

    private KeyName(string value) => Value = value;

    public static KeyName Create(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidValueException("Key name must not be empty.")
            : new KeyName(value);

    public override string ToString() => Value;
}
