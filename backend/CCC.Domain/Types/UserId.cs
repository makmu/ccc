namespace CCC.Domain.Types;

public readonly record struct UserId
{
    public Guid Value { get; }

    private UserId(Guid value) => Value = value;

    public static UserId Create(Guid value) =>
        value == Guid.Empty
            ? throw new InvalidValueException("User id must not be empty.")
            : new UserId(value);

    public override string ToString() => Value.ToString();
}
