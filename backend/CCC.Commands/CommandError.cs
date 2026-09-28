namespace CCC.Commands;

public enum CommandErrorKind
{
    NotFound,
    Conflict,
    Invalid,
}

public record CommandError(CommandErrorKind Kind, string Message);
