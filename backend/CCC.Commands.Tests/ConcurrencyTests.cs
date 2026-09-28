using CCC.Commands.Keys;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.ValueObjects;
using CCC.EventStore;
using static CCC.Commands.Tests.CommandFixture;

namespace CCC.Commands.Tests;

public class ConcurrencyTests
{
    private readonly CommandFixture _fixture = new();

    [Fact]
    public async Task A_write_into_the_loaded_state_between_load_and_append_is_a_conflict()
    {
        var (team, project) = await _fixture.GivenProject();
        var competingKey = NewKeyId();

        // Another writer takes the same key name after AddKey has loaded its state.
        _fixture.Store.BeforeConditionalAppend = () =>
        {
            _fixture.Store.BeforeConditionalAppend = null;
            _fixture.Store.Append(new KeyAdded(competingKey.Value, "title", project.Value),
                Tags.From(Tag.TeamId(team), Tag.ProjectId(project), Tag.KeyId(competingKey), Tag.KeyName(KeyName.Create("title"))));
        };

        var result = await _fixture.Handle(new AddKey(team, project, NewKeyId(), KeyName.Create("title")));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "A concurrent operation modified the event store. Please retry.");
        Assert.Equal(competingKey.Value, _fixture.LastEvent<KeyAdded>().Id);
    }

    [Fact]
    public async Task A_write_outside_the_loaded_state_does_not_block_the_append()
    {
        var (team, project) = await _fixture.GivenProject();
        var (_, otherProject) = await _fixture.GivenProject();

        // A key in an unrelated project touches none of the tags AddKey loaded.
        _fixture.Store.BeforeConditionalAppend = () =>
        {
            _fixture.Store.BeforeConditionalAppend = null;
            _fixture.Store.Append(new ReferenceTextProvided(Guid.NewGuid(), otherProject.Value, "Hello"),
                Tags.From(Tag.KeyId(NewKeyId())));
        };

        var result = await _fixture.Handle(new AddKey(team, project, NewKeyId(), KeyName.Create("title")));

        CommandAssert.Succeeded(result);
    }
}
