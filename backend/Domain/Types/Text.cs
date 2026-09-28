namespace CCC.Domain.Types;

public readonly record struct Text
{
    public string Value { get; }

    private Text(string value) => Value = value;

    public static Text From(string? value) =>
        value is null
            ? throw new InvalidValueException("Text must be provided.")
            : new Text(value);

    public override string ToString() => Value;
}
