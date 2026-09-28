namespace CCC.Domain.Types;

public readonly record struct ProjectName
{
    public string Value { get; }

    public string Canonical => CanonicalForm.Of(Value);

    private ProjectName(string value) => Value = value;

    public static ProjectName Create(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidValueException("Project name must not be empty.")
            : new ProjectName(value);

    public override string ToString() => Value;
}
