namespace CCC.Domain.ValueObjects;

public readonly record struct SubscriptionModelId
{
    public Guid Value { get; }

    private SubscriptionModelId(Guid value) => Value = value;

    public static SubscriptionModelId Create(Guid value) =>
        value == Guid.Empty
            ? throw new InvalidValueException("Subscription model id must not be empty.")
            : new SubscriptionModelId(value);

    public override string ToString() => Value.ToString();
}
