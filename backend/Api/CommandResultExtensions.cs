using CCC.Base.Abstractions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CCC.Api;

static class CommandResultExtensions
{
    public static Results<Ok, NotFound<ProblemDetails>, Conflict<ProblemDetails>, BadRequest<ProblemDetails>> ToHttpResult(
        this CommandResult result) => result.Error switch
    {
        null => TypedResults.Ok(),
        { Kind: CommandErrorKind.NotFound } error => TypedResults.NotFound(new ProblemDetails { Detail = error.Message }),
        { Kind: CommandErrorKind.Conflict } error => TypedResults.Conflict(new ProblemDetails { Detail = error.Message }),
        { Kind: CommandErrorKind.Invalid } error => TypedResults.BadRequest(new ProblemDetails { Detail = error.Message }),
        _ => throw new ArgumentOutOfRangeException(nameof(result), result.Error.Kind, "Unknown command error kind."),
    };
}
