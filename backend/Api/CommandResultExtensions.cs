using CCC.Base.Abstractions;

namespace CCC.Api;

static class CommandResultExtensions
{
    public static IResult ToHttpResult(this CommandResult result) => result.Error switch
    {
        null => Results.Ok(),
        { Kind: CommandErrorKind.NotFound } error => Results.NotFound(error.Message),
        { Kind: CommandErrorKind.Conflict } error => Results.Conflict(error.Message),
        { Kind: CommandErrorKind.Invalid } error => Results.BadRequest(error.Message),
        _ => throw new ArgumentOutOfRangeException(nameof(result), result.Error.Kind, "Unknown command error kind."),
    };
}
