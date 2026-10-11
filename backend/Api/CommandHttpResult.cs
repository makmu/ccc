using System.Reflection;
using CCC.Base.Abstractions;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace CCC.Api;

// ProblemHttpResult adds no response metadata, so this type declares the statuses it can write.
sealed class CommandHttpResult(CommandResult result) : IResult, IEndpointMetadataProvider
{
    public Task ExecuteAsync(HttpContext httpContext) => ToResult().ExecuteAsync(httpContext);

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(void), []));
        foreach (var statusCode in new[] { StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict })
            builder.Metadata.Add(new ProducesResponseTypeMetadata(statusCode, typeof(ProblemDetails), ["application/problem+json"]));
    }

    private IResult ToResult() => result.Error switch
    {
        null => TypedResults.Ok(),
        { Kind: CommandErrorKind.NotFound } error => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status404NotFound),
        { Kind: CommandErrorKind.Conflict } error => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status409Conflict),
        { Kind: CommandErrorKind.Invalid } error => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status400BadRequest),
        _ => throw new ArgumentOutOfRangeException(nameof(result), result.Error.Kind, "Unknown command error kind."),
    };
}
