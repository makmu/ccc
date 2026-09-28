namespace CCC.Domain.ValueObjects;

public readonly record struct ProjectName
{
    // As entered. This is what events store.
    public string Value { get; }

    // What uniqueness checks and tags compare.
    public string Canonical => CanonicalForm.Of(Value);

    private ProjectName(string value) => Value = value;

    public static ProjectName Create(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidValueException("Project name must not be empty.")
            : new ProjectName(value);

    public override string ToString() => Value;
}
