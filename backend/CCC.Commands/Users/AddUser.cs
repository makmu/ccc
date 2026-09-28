using CCC.Domain.Types;

namespace CCC.Commands.Users;

public record AddUser(UserId Id, string Name, EmailAddress Email);
