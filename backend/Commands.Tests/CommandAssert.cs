using CCC.Abstractions;

namespace CCC.Commands.Tests;

static class CommandAssert
{
    public static void Failed(CommandResult result, CommandErrorKind kind, string message)
    {
        Assert.False(result.Succeeded, "Expected the command to fail.");
        Assert.Equal(new CommandError(kind, message), result.Error);
    }

    public static void Succeeded(CommandResult result) =>
        Assert.True(result.Succeeded, $"Expected the command to succeed, but got {result.Error}.");
}
