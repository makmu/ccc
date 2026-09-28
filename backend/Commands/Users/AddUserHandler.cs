using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.EventStore;

namespace CCC.Commands.Users;

sealed class AddUserHandler(IStateLoader loader) : ICommandHandler<AddUser>
{
    public async Task<CommandResult> HandleAsync(AddUser command)
    {
        var idTaken = User.Exists(command.Id);
        var emailTaken = User.EmailTaken(command.Email);

        var context = await loader.LoadAsync(idTaken, emailTaken);

        if (idTaken)
            return CommandResult.Conflict("A user with this id already exists.");
        if (emailTaken)
            return CommandResult.Conflict("A user with this email address already exists.");

        return await context.TryAppendAsync(
            new UserAdded(command.Id.Value, command.Name, command.Email.Value),
            Tags.From(Tag.UserId(command.Id), Tag.UserEmail(command.Email)));
    }
}
