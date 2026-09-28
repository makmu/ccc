namespace CCC.Commands;

public interface ICommandHandler<in TCommand>
{
    Task<CommandResult> HandleAsync(TCommand command);
}
