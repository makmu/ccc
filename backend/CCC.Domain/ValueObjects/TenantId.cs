namespace CCC.Domain.ValueObjects;

public readonly record struct TenantId
{
    public Guid Value { get; }

    private TenantId(Guid value) => Value = value;

    public static TenantId Create(Guid value) =>
        value == Guid.Empty
            ? throw new InvalidValueException("Tenant id must not be empty.")
            : new TenantId(value);

    public override string ToString() => Value.ToString();
}
