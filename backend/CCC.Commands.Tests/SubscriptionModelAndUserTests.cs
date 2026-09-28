using CCC.Commands.SubscriptionModels;
using CCC.Commands.Users;
using CCC.Domain.Events;
using CCC.Domain.Types;
using static CCC.Commands.Tests.CommandFixture;

namespace CCC.Commands.Tests;

public class SubscriptionModelAndUserTests
{
    private readonly CommandFixture _fixture = new();

    [Fact]
    public async Task AddSubscriptionModel_appends_the_event()
    {
        var id = NewSubscriptionModelId();

        var result = await _fixture.Handle(new AddSubscriptionModel(id, "Basic", KeyLimit.Create(5)));

        CommandAssert.Succeeded(result);
        Assert.Equal(new SubscriptionModelAdded(id.Value, "Basic", 5), _fixture.LastEvent<SubscriptionModelAdded>());
        Assert.Equal([$"subscription-id={id}"], _fixture.LastTags());
    }

    [Fact]
    public async Task AddSubscriptionModel_rejects_a_taken_id()
    {
        var id = await _fixture.GivenSubscriptionModel();

        var result = await _fixture.Handle(new AddSubscriptionModel(id, "Other", KeyLimit.Create(1)));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "A subscription model with this id already exists.");
    }

    [Fact]
    public async Task AddUser_appends_the_event()
    {
        var id = NewUserId();

        var result = await _fixture.Handle(new AddUser(id, "Alice", EmailAddress.Create("alice@example.com")));

        CommandAssert.Succeeded(result);
        Assert.Equal(new UserAdded(id.Value, "Alice", "alice@example.com"), _fixture.LastEvent<UserAdded>());
    }

    [Fact]
    public async Task AddUser_rejects_a_taken_id()
    {
        var id = await _fixture.GivenUser();

        var result = await _fixture.Handle(new AddUser(id, "Bob", EmailAddress.Create("bob@example.com")));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "A user with this id already exists.");
    }

    [Fact]
    public async Task AddUser_rejects_a_taken_email()
    {
        await _fixture.GivenUser("alice@example.com");

        var result = await _fixture.Handle(new AddUser(NewUserId(), "Alice 2", EmailAddress.Create("alice@example.com")));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "A user with this email address already exists.");
    }
}
