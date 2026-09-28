namespace CCC.Domain.ValueObjects;

public readonly record struct LanguageCode
{
    public string Value { get; }

    private LanguageCode(string value) => Value = value;

    public static LanguageCode Create(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidValueException("Language code must not be empty.")
            : new LanguageCode(value);

    public override string ToString() => Value;
}
