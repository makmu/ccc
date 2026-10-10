namespace CCC.Abstractions;

public sealed record CommandResult
{
    private CommandResult(CommandError? error) => Error = error;

    public CommandError? Error { get; }

    public bool Succeeded => Error is null;

    public static CommandResult Ok { get; } = new((CommandError?)null);

    public static CommandResult NotFound(string message) => new(new CommandError(CommandErrorKind.NotFound, message));

    public static CommandResult Conflict(string message) => new(new CommandError(CommandErrorKind.Conflict, message));

    public static CommandResult Invalid(string message) => new(new CommandError(CommandErrorKind.Invalid, message));
}
