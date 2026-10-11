namespace CCC.Domain.Types;

public readonly record struct EmailAddress
{
    public string Value { get; }

    private EmailAddress(string value) => Value = value;

    public static EmailAddress From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidValueException("Email address must not be empty.");

        value = value.Trim();
        var at = value.IndexOf('@');
        if (at <= 0 || at == value.Length - 1 || value.Any(char.IsWhiteSpace))
            throw new InvalidValueException("Email address is not valid.");

        return new EmailAddress(value);
    }

    public override string ToString() => Value;
}
