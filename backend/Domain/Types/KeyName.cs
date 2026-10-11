namespace CCC.Domain.Types;

public readonly record struct KeyName
{
    public string Value { get; }

    private KeyName(string value) => Value = value;

    public static KeyName From(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidValueException("Key name must not be empty.")
            : new KeyName(value.Trim());

    public override string ToString() => Value;
}
