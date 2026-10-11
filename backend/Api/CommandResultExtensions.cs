using CCC.Base.Abstractions;

namespace CCC.Api;

static class CommandResultExtensions
{
    public static CommandHttpResult ToHttpResult(this CommandResult result) => new(result);
}
