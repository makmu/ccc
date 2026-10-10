using CCC.Abstractions;
using CCC.EventStore;

namespace CCC.Commands;

static class StateContextExtensions
{
    // Generic on purpose: StateContext stores typeof(TEvent).Name as the event
    // type, so the event must not be passed along as object.
    public static async Task<CommandResult> TryAppendAsync<TEvent>(this StateContext context, TEvent @event, Tags tags)
    {
        try
        {
            await context.AppendAsync(@event, tags);
            return CommandResult.Ok;
        }
        catch (ConcurrencyException)
        {
            return CommandResult.Conflict("A concurrent operation modified the event store. Please retry.");
        }
    }
}
