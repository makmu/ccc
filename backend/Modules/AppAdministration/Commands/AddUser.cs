using CCC.Abstractions;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.AppAdministration.Commands;

public record AddUser(UserId Id, string Name, EmailAddress Email);

sealed class AddUserHandler(IStateLoader loader) : ICommandHandler<AddUser>
{
    public async Task<CommandResult> HandleAsync(AddUser command)
    {
        var (idTaken, emailTaken, context) = await loader.LoadAsync(
            User.Exists(command.Id),
            User.EmailTaken(command.Email));

        if (idTaken)
            return CommandResult.Conflict("A user with this id already exists.");

        if (emailTaken)
            return CommandResult.Conflict("A user with this email address already exists.");

        var appended = await context.TryAppendAsync(
            new UserAdded(command.Id.Value, command.Name, command.Email.Value),
            Tags.From(Tag.UserId(command.Id), Tag.UserEmail(command.Email)));
        return appended ? CommandResult.Ok : CommandResult.ConcurrentModification;
    }
}
