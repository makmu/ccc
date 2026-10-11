namespace CCC.Base.Abstractions;

public interface ICommandHandler<in TCommand>
{
    Task<CommandResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
