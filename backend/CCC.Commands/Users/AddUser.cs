using CCC.Domain.ValueObjects;

namespace CCC.Commands.Users;

public record AddUser(UserId Id, string Name, EmailAddress Email);
