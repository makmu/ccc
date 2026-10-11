using System.Data;
using Dapper;

namespace CCC.EventStore;

public static class CommandDefinitionExtensions
{
    public static CommandDefinition With(this CommandDefinition command, CancellationToken cancellationToken) =>
        command.With(null, cancellationToken);

    public static CommandDefinition With(this CommandDefinition command, IDbTransaction? transaction, CancellationToken cancellationToken) =>
        new(command.CommandText, command.Parameters, transaction, command.CommandTimeout, command.CommandType, command.Flags,
            cancellationToken);
}
