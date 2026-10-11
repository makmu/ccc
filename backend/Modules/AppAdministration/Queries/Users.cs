using CCC.Base.Abstractions;
using CCC.Base.Projections;
using Dapper;

namespace CCC.Modules.AppAdministration.Queries;

public record Users;

public record UserSummary(Guid Id, string Name, string Email);

sealed class UsersHandler(ProjectionDatabase database) : IQueryHandler<Users, IReadOnlyList<UserSummary>>
{
    public async Task<IReadOnlyList<UserSummary>> HandleAsync(Users query, CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        var users = await connection.QueryAsync<UserSummary>(new CommandDefinition("""
            SELECT user_id AS Id, name AS Name, email AS Email
            FROM app_administration.users
            ORDER BY name
            """, cancellationToken: cancellationToken));
        return users.AsList();
    }
}
