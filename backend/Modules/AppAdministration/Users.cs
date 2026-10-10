using CCC.Abstractions;
using CCC.EventStore;
using Dapper;

namespace CCC.Modules.AppAdministration;

public record Users;

public record UserSummary(Guid Id, string Name, string Email);

sealed class UsersHandler(ReadModelDatabase database) : IQueryHandler<Users, IReadOnlyList<UserSummary>>
{
    public async Task<IReadOnlyList<UserSummary>> HandleAsync(Users query)
    {
        await using var connection = await database.OpenAsync();
        var users = await connection.QueryAsync<UserSummary>("""
            SELECT user_id AS Id, name AS Name, email AS Email
            FROM app_administration.users
            ORDER BY name
            """);
        return users.AsList();
    }
}
