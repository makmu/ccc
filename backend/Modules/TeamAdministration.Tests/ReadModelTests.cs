using CCC.Domain.Events;
using CCC.Domain.Types;
using CCC.Modules.TeamAdministration.Queries;
using CCC.TestKit;

namespace CCC.Modules.TeamAdministration.Tests;

public class ReadModelTests : IAsyncLifetime
{
    private readonly ReadModelFixture _fixture = new("team_administration", services => services.AddTeamAdministration());
    private readonly Guid _team = Guid.NewGuid();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task A_team_without_projects_lists_none()
    {
        Assert.Empty(await ProjectsOf(_team));
    }

    [Fact]
    public async Task A_new_project_has_only_its_reference_language()
    {
        var project = Guid.NewGuid();
        await _fixture.Given(new ProjectAdded(project, "Web", _team, "en"));

        var listed = Assert.Single(await ProjectsOf(_team));

        Assert.Equal((project, "Web", "en"), (listed.Id, listed.Name, listed.ReferenceLanguage));
        Assert.Equal([new ProjectLanguage("en", null)], listed.Languages);
    }

    [Fact]
    public async Task Languages_are_listed_with_their_parent_language()
    {
        var project = Guid.NewGuid();
        await _fixture.Given(new ProjectAdded(project, "Web", _team, "en"));
        await _fixture.Given(new LanguageAdded("de", "en", project));
        await _fixture.Given(new LanguageAdded("de-AT", "de", project));

        var listed = Assert.Single(await ProjectsOf(_team));

        Assert.Equal(
            [new ProjectLanguage("de", "en"), new ProjectLanguage("de-AT", "de"), new ProjectLanguage("en", null)],
            listed.Languages);
    }

    [Fact]
    public async Task Only_the_teams_own_projects_are_listed_by_name()
    {
        await _fixture.Given(new ProjectAdded(Guid.NewGuid(), "Web", _team, "en"));
        await _fixture.Given(new ProjectAdded(Guid.NewGuid(), "App", _team, "de"));
        await _fixture.Given(new ProjectAdded(Guid.NewGuid(), "Other", Guid.NewGuid(), "en"));

        Assert.Equal(["App", "Web"], (await ProjectsOf(_team)).Select(project => project.Name));
    }

    private Task<IReadOnlyList<ProjectSummary>> ProjectsOf(Guid team) =>
        _fixture.Query<ProjectsOfTeam, IReadOnlyList<ProjectSummary>>(new ProjectsOfTeam(TeamId.From(team)));
}
