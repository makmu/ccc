using System.Data;
using Dapper;

namespace CCC.Base.ModuleHosting;

public static class CommandDefinitionExtensions
{
    public static CommandDefinition With(this CommandDefinition statement, CancellationToken cancellationToken) =>
        statement.With(null, cancellationToken);

    public static CommandDefinition With(this CommandDefinition statement, IDbTransaction? transaction, CancellationToken cancellationToken) =>
        new(statement.CommandText, statement.Parameters, transaction, statement.CommandTimeout, statement.CommandType, statement.Flags,
            cancellationToken);
}
