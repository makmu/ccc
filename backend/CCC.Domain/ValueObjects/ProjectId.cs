namespace CCC.Domain.ValueObjects;

public readonly record struct ProjectId
{
    public Guid Value { get; }

    private ProjectId(Guid value) => Value = value;

    public static ProjectId Create(Guid value) =>
        value == Guid.Empty
            ? throw new InvalidValueException("Project id must not be empty.")
            : new ProjectId(value);

    public override string ToString() => Value.ToString();
}
