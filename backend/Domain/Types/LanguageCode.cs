namespace CCC.Domain.Types;

public readonly record struct LanguageCode
{
    public string Value { get; }

    private LanguageCode(string value) => Value = value;

    public static LanguageCode From(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidValueException("Language code must not be empty.")
            : new LanguageCode(value.Trim());

    public override string ToString() => Value;
}
