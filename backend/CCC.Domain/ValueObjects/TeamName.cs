namespace CCC.Domain.ValueObjects;

public readonly record struct TeamName
{
    // As entered. This is what events store.
    public string Value { get; }

    // What uniqueness checks and tags compare.
    public string Canonical => CanonicalForm.Of(Value);

    private TeamName(string value) => Value = value;

    public static TeamName Create(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidValueException("Team name must not be empty.")
            : new TeamName(value);

    public override string ToString() => Value;
}
