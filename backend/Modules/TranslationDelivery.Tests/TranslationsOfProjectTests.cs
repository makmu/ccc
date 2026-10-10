using CCC.Domain.Events;

namespace CCC.Modules.TranslationDelivery.Tests;

public class TranslationsOfProjectTests : IAsyncLifetime
{
    private readonly ReadModelFixture _fixture = new();
    private readonly Guid _team = Guid.NewGuid();
    private readonly Guid _project = Guid.NewGuid();
    private readonly Guid _key = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        await _fixture.Given(new ProjectAdded(_project, "Web", _team, "en"));
        await _fixture.Given(new LanguageAdded("de", "en", _project));
        await _fixture.Given(new LanguageAdded("de-AT", "de", _project));
        await _fixture.Given(new LanguageAdded("fr", "en", _project));
        await _fixture.Given(new KeyAdded(_key, "title", _project));
    }

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task A_key_without_any_text_has_no_text_in_any_language()
    {
        Assert.Equal(new Translation(_key, "title", null, null), await TitleIn("en"));
        Assert.Equal(new Translation(_key, "title", null, null), await TitleIn("de-AT"));
    }

    [Fact]
    public async Task Every_language_shows_the_reference_text_until_it_has_its_own()
    {
        await _fixture.Given(new ReferenceTextProvided(_key, _project, "Hello"));

        Assert.Equal(("Hello", "en"), await TextIn("en"));
        Assert.Equal(("Hello", "en"), await TextIn("de"));
        Assert.Equal(("Hello", "en"), await TextIn("de-AT"));
        Assert.Equal(("Hello", "en"), await TextIn("fr"));
    }

    [Fact]
    public async Task A_translation_is_inherited_by_the_languages_below_it_only()
    {
        await _fixture.Given(new ReferenceTextProvided(_key, _project, "Hello"));
        await _fixture.Given(new TranslationProvided(_key, _project, "de", "Hallo"));

        Assert.Equal(("Hallo", "de"), await TextIn("de"));
        Assert.Equal(("Hallo", "de"), await TextIn("de-AT"));
        Assert.Equal(("Hello", "en"), await TextIn("fr"));
        Assert.Equal(("Hello", "en"), await TextIn("en"));
    }

    [Fact]
    public async Task A_changed_text_does_not_reach_a_language_with_its_own_text_or_the_languages_below_it()
    {
        await _fixture.Given(new LanguageAdded("de-AT-W", "de-AT", _project));
        await _fixture.Given(new TranslationProvided(_key, _project, "de-AT", "Servus"));
        await _fixture.Given(new TranslationProvided(_key, _project, "de", "Hallo"));
        await _fixture.Given(new ReferenceTextProvided(_key, _project, "Hi"));

        Assert.Equal(("Hi", "en"), await TextIn("fr"));
        Assert.Equal(("Hallo", "de"), await TextIn("de"));
        Assert.Equal(("Servus", "de-AT"), await TextIn("de-AT"));
        Assert.Equal(("Servus", "de-AT"), await TextIn("de-AT-W"));
    }

    [Fact]
    public async Task A_changed_text_reaches_every_inheriting_language_further_down()
    {
        await _fixture.Given(new LanguageAdded("de-AT-W", "de-AT", _project));
        await _fixture.Given(new TranslationProvided(_key, _project, "de", "Hallo"));
        await _fixture.Given(new TranslationProvided(_key, _project, "de", "Guten Tag"));

        Assert.Equal(("Guten Tag", "de"), await TextIn("de-AT-W"));
    }

    [Fact]
    public async Task A_language_added_later_starts_with_the_texts_of_its_parent()
    {
        await _fixture.Given(new ReferenceTextProvided(_key, _project, "Hello"));
        await _fixture.Given(new TranslationProvided(_key, _project, "de", "Hallo"));
        await _fixture.Given(new LanguageAdded("de-CH", "de", _project));

        Assert.Equal(("Hallo", "de"), await TextIn("de-CH"));
    }

    [Fact]
    public async Task A_key_added_later_inherits_texts_like_any_other_key()
    {
        var subtitle = Guid.NewGuid();
        await _fixture.Given(new KeyAdded(subtitle, "subtitle", _project));
        await _fixture.Given(new ReferenceTextProvided(subtitle, _project, "Welcome"));

        var translations = await _fixture.TranslationsOf(_team, _project, "de-AT");

        Assert.Equal(
            [new Translation(subtitle, "subtitle", "Welcome", "en"), new Translation(_key, "title", null, null)],
            translations);
    }

    [Fact]
    public async Task A_renamed_key_shows_its_new_name_in_every_language()
    {
        await _fixture.Given(new KeyRenamed(_key, _project, "heading"));

        Assert.Equal("heading", (await TitleIn("en")).KeyName);
        Assert.Equal("heading", (await TitleIn("fr")).KeyName);
    }

    [Fact]
    public async Task Texts_of_another_project_do_not_show_up()
    {
        var otherProject = Guid.NewGuid();
        await _fixture.Given(new ProjectAdded(otherProject, "App", _team, "en"));
        await _fixture.Given(new KeyAdded(Guid.NewGuid(), "title", otherProject));

        Assert.Single((await _fixture.TranslationsOf(_team, _project, "en"))!);
    }

    [Fact]
    public async Task A_language_the_project_does_not_have_is_not_found()
    {
        Assert.Null(await _fixture.TranslationsOf(_team, _project, "it"));
    }

    [Fact]
    public async Task A_project_of_another_team_is_not_found()
    {
        Assert.Null(await _fixture.TranslationsOf(Guid.NewGuid(), _project, "en"));
    }

    private async Task<Translation> TitleIn(string languageCode) =>
        Assert.Single((await _fixture.TranslationsOf(_team, _project, languageCode))!);

    private async Task<(string?, string?)> TextIn(string languageCode)
    {
        var title = await TitleIn(languageCode);
        return (title.Text, title.SourceLanguageCode);
    }
}
