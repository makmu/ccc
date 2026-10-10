namespace CCC.Abstractions;

public interface ICommandHandler<in TCommand>
{
    Task<CommandResult> HandleAsync(TCommand command);
}
