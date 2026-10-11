using CCC.Abstractions;
using CCC.Domain.Events;
using CCC.Domain.Types;
using CCC.Modules.TranslationEditor.Commands;
using CCC.TestKit;
using static CCC.TestKit.CommandFixture;

namespace CCC.Modules.TranslationEditor.Tests;

public class KeyTests
{
    private readonly CommandFixture _fixture = new();

    [Fact]
    public async Task AddKey_appends_the_event()
    {
        var (team, project) = await _fixture.GivenProject();
        var key = NewKeyId();

        var result = await _fixture.Handle(new AddKey(team, project, key, KeyName.From("Title")));

        CommandAssert.Succeeded(result);
        Assert.Equal(new KeyAdded(key.Value, "Title", project.Value), _fixture.LastEvent<KeyAdded>());
        Assert.Contains("key-name=Title", _fixture.LastTags());
    }

    [Fact]
    public async Task AddKey_rejects_an_unknown_team()
    {
        var (_, project) = await _fixture.GivenProject();

        var result = await _fixture.Handle(new AddKey(NewTeamId(), project, NewKeyId(), KeyName.From("title")));

        CommandAssert.Failed(result, CommandErrorKind.NotFound, "Team not found.");
    }

    [Fact]
    public async Task AddKey_rejects_a_project_outside_the_team()
    {
        var (team, _) = await _fixture.GivenProject();

        var result = await _fixture.Handle(new AddKey(team, NewProjectId(), NewKeyId(), KeyName.From("title")));

        CommandAssert.Failed(result, CommandErrorKind.NotFound, "Project not found in this team.");
    }

    [Fact]
    public async Task AddKey_rejects_a_taken_id()
    {
        var (team, project, key) = await _fixture.GivenKey("title");

        var result = await _fixture.Handle(new AddKey(team, project, key, KeyName.From("other")));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "Key ID already taken.");
    }

    [Fact]
    public async Task AddKey_rejects_an_id_taken_in_another_project()
    {
        var (_, _, key) = await _fixture.GivenKey("title");
        var (team, project) = await _fixture.GivenProject();

        var result = await _fixture.Handle(new AddKey(team, project, key, KeyName.From("other")));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "Key ID already taken.");
    }

    [Fact]
    public async Task AddKey_rejects_a_taken_name()
    {
        var (team, project, _) = await _fixture.GivenKey("title");

        var result = await _fixture.Handle(new AddKey(team, project, NewKeyId(), KeyName.From("title")));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "Key name already taken.");
    }

    [Fact]
    public async Task AddKey_enforces_the_key_limit_of_the_team_subscription()
    {
        var (team, project, _) = await _fixture.GivenKey("title", keyLimit: 2);
        await _fixture.Given(new AddKey(team, project, NewKeyId(), KeyName.From("subtitle")));

        var result = await _fixture.Handle(new AddKey(team, project, NewKeyId(), KeyName.From("third")));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "The key limit for this team's subscription model has been reached.");
    }

    [Fact]
    public async Task RenameKey_tags_both_names()
    {
        var (team, project, key) = await _fixture.GivenKey("title");

        var result = await _fixture.Handle(new RenameKey(team, project, key, KeyName.From("heading")));

        CommandAssert.Succeeded(result);
        Assert.Equal(new KeyRenamed(key.Value, project.Value, "heading"), _fixture.LastEvent<KeyRenamed>());
        Assert.Contains("key-name=title", _fixture.LastTags());
        Assert.Contains("key-name=heading", _fixture.LastTags());
    }

    [Fact]
    public async Task RenameKey_rejects_an_unknown_key()
    {
        var (team, project) = await _fixture.GivenProject();

        var result = await _fixture.Handle(new RenameKey(team, project, NewKeyId(), KeyName.From("heading")));

        CommandAssert.Failed(result, CommandErrorKind.NotFound, "Key not found in this project.");
    }

    [Fact]
    public async Task RenameKey_rejects_a_taken_name()
    {
        var (team, project, key) = await _fixture.GivenKey("title");
        await _fixture.Given(new AddKey(team, project, NewKeyId(), KeyName.From("subtitle")));

        var result = await _fixture.Handle(new RenameKey(team, project, key, KeyName.From("subtitle")));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "A key with this name already exists in this project.");
    }

    [Fact]
    public async Task RenameKey_to_its_own_name_appends_nothing()
    {
        var (team, project, key) = await _fixture.GivenKey("title");
        var eventsBefore = _fixture.Store.Events.Count;

        var result = await _fixture.Handle(new RenameKey(team, project, key, KeyName.From("title")));

        CommandAssert.Succeeded(result);
        Assert.Equal(eventsBefore, _fixture.Store.Events.Count);
    }

    [Fact]
    public async Task ProvideReferenceText_appends_the_event()
    {
        var (team, project, key) = await _fixture.GivenKey();

        var result = await _fixture.Handle(new ProvideReferenceText(team, project, key, Text.From("Hello")));

        CommandAssert.Succeeded(result);
        Assert.Equal(new ReferenceTextProvided(key.Value, project.Value, "Hello"), _fixture.LastEvent<ReferenceTextProvided>());
    }

    [Fact]
    public async Task ProvideReferenceText_with_unchanged_text_appends_nothing()
    {
        var (team, project, key) = await _fixture.GivenKey();
        await _fixture.Given(new ProvideReferenceText(team, project, key, Text.From("Hello")));
        var eventsBefore = _fixture.Store.Events.Count;

        var result = await _fixture.Handle(new ProvideReferenceText(team, project, key, Text.From("Hello")));

        CommandAssert.Succeeded(result);
        Assert.Equal(eventsBefore, _fixture.Store.Events.Count);
    }

    [Fact]
    public async Task ProvideReferenceText_with_empty_text_for_a_key_without_one_appends_it()
    {
        var (team, project, key) = await _fixture.GivenKey();

        var result = await _fixture.Handle(new ProvideReferenceText(team, project, key, Text.From("")));

        CommandAssert.Succeeded(result);
        Assert.Equal(new ReferenceTextProvided(key.Value, project.Value, ""), _fixture.LastEvent<ReferenceTextProvided>());
    }

    [Fact]
    public async Task ProvideReferenceText_without_text_withdraws_the_reference_text()
    {
        var (team, project, key) = await _fixture.GivenKey();
        await _fixture.Given(new ProvideReferenceText(team, project, key, Text.From("Hello")));

        var result = await _fixture.Handle(new ProvideReferenceText(team, project, key, null));

        CommandAssert.Succeeded(result);
        Assert.Equal(new ReferenceTextWithdrawn(key.Value, project.Value), _fixture.LastEvent<ReferenceTextWithdrawn>());
    }

    [Fact]
    public async Task ProvideReferenceText_without_text_for_a_key_without_one_appends_nothing()
    {
        var (team, project, key) = await _fixture.GivenKey();
        var eventsBefore = _fixture.Store.Events.Count;

        var result = await _fixture.Handle(new ProvideReferenceText(team, project, key, null));

        CommandAssert.Succeeded(result);
        Assert.Equal(eventsBefore, _fixture.Store.Events.Count);
    }

    [Fact]
    public async Task ProvideReferenceText_rejects_an_unknown_key()
    {
        var (team, project) = await _fixture.GivenProject();

        var result = await _fixture.Handle(new ProvideReferenceText(team, project, NewKeyId(), Text.From("Hello")));

        CommandAssert.Failed(result, CommandErrorKind.NotFound, "Key not found in this project.");
    }

    [Fact]
    public async Task ProvideTranslation_appends_the_event()
    {
        var (team, project, key) = await _fixture.GivenKey();
        await _fixture.GivenLanguage(team, project, "de");

        var result = await _fixture.Handle(new ProvideTranslation(team, project, key, LanguageCode.From("de"), Text.From("Hallo")));

        CommandAssert.Succeeded(result);
        Assert.Equal(new TranslationProvided(key.Value, project.Value, "de", "Hallo"), _fixture.LastEvent<TranslationProvided>());
    }

    [Fact]
    public async Task ProvideTranslation_with_unchanged_text_appends_nothing()
    {
        var (team, project, key) = await _fixture.GivenKey();
        await _fixture.GivenLanguage(team, project, "de");
        await _fixture.Given(new ProvideTranslation(team, project, key, LanguageCode.From("de"), Text.From("Hallo")));
        var eventsBefore = _fixture.Store.Events.Count;

        var result = await _fixture.Handle(new ProvideTranslation(team, project, key, LanguageCode.From("de"), Text.From("Hallo")));

        CommandAssert.Succeeded(result);
        Assert.Equal(eventsBefore, _fixture.Store.Events.Count);
    }

    [Fact]
    public async Task ProvideTranslation_with_empty_text_for_a_language_without_one_appends_it()
    {
        var (team, project, key) = await _fixture.GivenKey();
        await _fixture.GivenLanguage(team, project, "de");

        var result = await _fixture.Handle(new ProvideTranslation(team, project, key, LanguageCode.From("de"), Text.From("")));

        CommandAssert.Succeeded(result);
        Assert.Equal(new TranslationProvided(key.Value, project.Value, "de", ""), _fixture.LastEvent<TranslationProvided>());
    }

    [Fact]
    public async Task ProvideTranslation_without_text_withdraws_the_translation()
    {
        var (team, project, key) = await _fixture.GivenKey();
        await _fixture.GivenLanguage(team, project, "de");
        await _fixture.Given(new ProvideTranslation(team, project, key, LanguageCode.From("de"), Text.From("Hallo")));

        var result = await _fixture.Handle(new ProvideTranslation(team, project, key, LanguageCode.From("de"), null));

        CommandAssert.Succeeded(result);
        Assert.Equal(new TranslationWithdrawn(key.Value, project.Value, "de"), _fixture.LastEvent<TranslationWithdrawn>());
        Assert.Contains("language-code=de", _fixture.LastTags());
    }

    [Fact]
    public async Task ProvideTranslation_without_text_for_a_language_without_one_appends_nothing()
    {
        var (team, project, key) = await _fixture.GivenKey();
        await _fixture.GivenLanguage(team, project, "de");
        await _fixture.Given(new ProvideTranslation(team, project, key, LanguageCode.From("de"), Text.From("Hallo")));
        await _fixture.Given(new ProvideTranslation(team, project, key, LanguageCode.From("de"), null));
        var eventsBefore = _fixture.Store.Events.Count;

        var result = await _fixture.Handle(new ProvideTranslation(team, project, key, LanguageCode.From("de"), null));

        CommandAssert.Succeeded(result);
        Assert.Equal(eventsBefore, _fixture.Store.Events.Count);
    }

    [Fact]
    public async Task ProvideTranslation_rejects_a_language_the_project_does_not_have()
    {
        var (team, project, key) = await _fixture.GivenKey();

        var result = await _fixture.Handle(new ProvideTranslation(team, project, key, LanguageCode.From("fr"), Text.From("Bonjour")));

        CommandAssert.Failed(result, CommandErrorKind.Invalid, "The specified language has not been added to this project.");
    }
}
