namespace CCC.Domain.ValueObjects;

public readonly record struct KeyLimit
{
    public int Value { get; }

    private KeyLimit(int value) => Value = value;

    public static KeyLimit Create(int value) =>
        value < 0
            ? throw new InvalidValueException("Key limit must not be negative.")
            : new KeyLimit(value);

    public override string ToString() => Value.ToString();
}
