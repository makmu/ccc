namespace CCC.Domain.Types;

public readonly record struct KeyId
{
    public Guid Value { get; }

    private KeyId(Guid value) => Value = value;

    public static KeyId From(Guid value) =>
        value == Guid.Empty
            ? throw new InvalidValueException("Key id must not be empty.")
            : new KeyId(value);

    public override string ToString() => Value.ToString();
}
