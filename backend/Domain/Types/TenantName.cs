namespace CCC.Domain.Types;

public readonly record struct TenantName
{
    public string Value { get; }

    public string Canonical => CanonicalForm.Of(Value);

    private TenantName(string value) => Value = value;

    public static TenantName Create(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidValueException("Tenant name must not be empty.")
            : new TenantName(value);

    public override string ToString() => Value;
}
