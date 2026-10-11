namespace CCC.Domain.Types;

public readonly record struct SubscriptionModelName
{
    public string Value { get; }

    private SubscriptionModelName(string value) => Value = value;

    public static SubscriptionModelName From(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidValueException("Subscription model name must not be empty.")
            : new SubscriptionModelName(value);

    public override string ToString() => Value;
}
