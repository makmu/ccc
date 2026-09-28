namespace CCC.Domain.Types;

public readonly record struct TeamId
{
    public Guid Value { get; }

    private TeamId(Guid value) => Value = value;

    public static TeamId Create(Guid value) =>
        value == Guid.Empty
            ? throw new InvalidValueException("Team id must not be empty.")
            : new TeamId(value);

    public override string ToString() => Value.ToString();
}
