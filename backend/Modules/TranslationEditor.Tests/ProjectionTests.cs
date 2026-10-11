using CCC.Domain.Events;
using CCC.Domain.Types;
using CCC.Modules.TranslationEditor.Queries;
using CCC.TestKit;

namespace CCC.Modules.TranslationEditor.Tests;

public class ProjectionTests : IAsyncLifetime
{
    private readonly ProjectionFixture _fixture = new("translation_editor", services => services.AddTranslationEditor());
    private readonly Guid _team = Guid.NewGuid();
    private readonly Guid _project = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        await _fixture.Given(new ProjectAdded(_project, "Web", _team, "en"));
        await _fixture.Given(new LanguageAdded("fr", "en", _project));
        await _fixture.Given(new LanguageAdded("de", "en", _project));
    }

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task A_project_of_another_team_has_no_keys_to_show()
    {
        Assert.Null(await KeysOf(Guid.NewGuid(), _project));
    }

    [Fact]
    public async Task A_new_project_shows_its_languages_and_no_keys()
    {
        var keys = await KeysOf(_team, _project);

        Assert.NotNull(keys);
        Assert.Equal("en", keys.ReferenceLanguage);
        Assert.Equal(["de", "fr"], keys.Languages);
        Assert.Empty(keys.Keys);
    }

    [Fact]
    public async Task Keys_are_listed_by_their_current_name_with_their_own_texts()
    {
        var title = Guid.NewGuid();
        var subtitle = Guid.NewGuid();
        await _fixture.Given(new KeyAdded(title, "title", _project));
        await _fixture.Given(new KeyAdded(subtitle, "subtitle", _project));
        await _fixture.Given(new KeyRenamed(subtitle, _project, "heading"));
        await _fixture.Given(new ReferenceTextProvided(title, _project, "Hello"));
        await _fixture.Given(new TranslationProvided(title, _project, "de", "Hallo"));

        var keys = (await KeysOf(_team, _project))!.Keys;

        Assert.Equal(["heading", "title"], keys.Select(key => key.Name));
        Assert.Equal((subtitle, null), (keys[0].Id, keys[0].ReferenceText));
        Assert.Empty(keys[0].Translations);
        Assert.Equal((title, "Hello"), (keys[1].Id, keys[1].ReferenceText));
        Assert.Equal(new Dictionary<string, string> { ["de"] = "Hallo" }, keys[1].Translations);
    }

    [Fact]
    public async Task A_provided_text_replaces_the_previous_one()
    {
        var title = Guid.NewGuid();
        await _fixture.Given(new KeyAdded(title, "title", _project));
        await _fixture.Given(new ReferenceTextProvided(title, _project, "Hello"));
        await _fixture.Given(new ReferenceTextProvided(title, _project, "Hi"));
        await _fixture.Given(new TranslationProvided(title, _project, "de", "Hallo"));
        await _fixture.Given(new TranslationProvided(title, _project, "de", "Servus"));

        var key = Assert.Single((await KeysOf(_team, _project))!.Keys);

        Assert.Equal("Hi", key.ReferenceText);
        Assert.Equal("Servus", key.Translations["de"]);
    }

    [Fact]
    public async Task A_withdrawn_text_is_no_longer_shown()
    {
        var title = Guid.NewGuid();
        await _fixture.Given(new KeyAdded(title, "title", _project));
        await _fixture.Given(new ReferenceTextProvided(title, _project, "Hello"));
        await _fixture.Given(new TranslationProvided(title, _project, "de", "Hallo"));
        await _fixture.Given(new TranslationProvided(title, _project, "fr", ""));
        await _fixture.Given(new ReferenceTextWithdrawn(title, _project));
        await _fixture.Given(new TranslationWithdrawn(title, _project, "de"));

        var key = Assert.Single((await KeysOf(_team, _project))!.Keys);

        Assert.Null(key.ReferenceText);
        Assert.Equal(new Dictionary<string, string> { ["fr"] = "" }, key.Translations);
    }

    private Task<ProjectKeys?> KeysOf(Guid team, Guid project) =>
        _fixture.Query<KeysOfProject, ProjectKeys?>(new KeysOfProject(TeamId.From(team), ProjectId.From(project)));
}
