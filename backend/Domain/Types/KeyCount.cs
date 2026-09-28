namespace CCC.Domain.Types;

public readonly record struct KeyCount
{
    public int Value { get; }

    private KeyCount(int value) => Value = value;

    public static KeyCount Zero { get; } = new(0);

    public static KeyCount From(int value) =>
        value < 0
            ? throw new InvalidValueException("Key count must not be negative.")
            : new KeyCount(value);

    public KeyCount Increment() => new(Value + 1);

    public static bool operator >=(KeyCount count, KeyLimit limit) => count.Value >= limit.Value;

    public static bool operator <=(KeyCount count, KeyLimit limit) => count.Value <= limit.Value;

    public override string ToString() => Value.ToString();
}
