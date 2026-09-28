namespace CCC.Domain.Types;

public readonly record struct TeamName
{
    public string Value { get; }

    public string Canonical => CanonicalForm.Of(Value);

    private TeamName(string value) => Value = value;

    public static TeamName Create(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidValueException("Team name must not be empty.")
            : new TeamName(value);

    public override string ToString() => Value;
}
