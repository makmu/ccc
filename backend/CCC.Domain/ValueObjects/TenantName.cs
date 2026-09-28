namespace CCC.Domain.ValueObjects;

public readonly record struct TenantName
{
    // As entered. This is what events store.
    public string Value { get; }

    // What uniqueness checks and tags compare.
    public string Canonical => CanonicalForm.Of(Value);

    private TenantName(string value) => Value = value;

    public static TenantName Create(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidValueException("Tenant name must not be empty.")
            : new TenantName(value);

    public override string ToString() => Value;
}
