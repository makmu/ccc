using CCC.Abstractions;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;
using CCC.Modules.TranslationEditor.Commands;
using CCC.TestKit;
using static CCC.TestKit.CommandFixture;

namespace CCC.Modules.TranslationEditor.Tests;

public class ConcurrencyTests
{
    private readonly CommandFixture _fixture = new();

    [Fact]
    public async Task A_write_into_the_loaded_state_between_load_and_append_is_a_conflict()
    {
        var (team, project) = await _fixture.GivenProject();
        var competingKey = NewKeyId();

        _fixture.Store.BeforeConditionalAppend = () =>
        {
            _fixture.Store.BeforeConditionalAppend = null;
            _fixture.Store.Append(new KeyAdded(competingKey.Value, "title", project.Value),
                Tags.From(Tag.TeamId(team), Tag.ProjectId(project), Tag.KeyId(competingKey), Tag.KeyName(KeyName.From("title"))));
        };

        var result = await _fixture.Handle(new AddKey(team, project, NewKeyId(), KeyName.From("title")));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "A concurrent operation modified the event store. Please retry.");
        Assert.Equal(competingKey.Value, _fixture.LastEvent<KeyAdded>().Id);
    }

    [Fact]
    public async Task A_write_outside_the_loaded_state_does_not_block_the_append()
    {
        var (team, project) = await _fixture.GivenProject();
        var (_, otherProject) = await _fixture.GivenProject();

        _fixture.Store.BeforeConditionalAppend = () =>
        {
            _fixture.Store.BeforeConditionalAppend = null;
            _fixture.Store.Append(new ReferenceTextProvided(Guid.NewGuid(), otherProject.Value, "Hello"),
                Tags.From(Tag.KeyId(NewKeyId())));
        };

        var result = await _fixture.Handle(new AddKey(team, project, NewKeyId(), KeyName.From("title")));

        CommandAssert.Succeeded(result);
    }
}
