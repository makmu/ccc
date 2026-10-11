namespace CCC.Domain.Types;

public readonly record struct UserName
{
    public string Value { get; }

    private UserName(string value) => Value = value;

    public static UserName From(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidValueException("User name must not be empty.")
            : new UserName(value);

    public override string ToString() => Value;
}
