using CCC.Base.Abstractions;
using CCC.Base.TestKit;
using CCC.Domain.Events;
using CCC.Domain.Types;
using CCC.Modules.TeamAdministration.Commands;
using static CCC.Base.TestKit.CommandFixture;

namespace CCC.Modules.TeamAdministration.Tests;

public class ProjectTests
{
    private readonly CommandFixture _fixture = new();

    [Fact]
    public async Task AddProject_appends_the_event_tagged_with_its_reference_language()
    {
        var (_, team) = await _fixture.GivenTeam();
        var project = NewProjectId();

        var result = await _fixture.Handle(new AddProject(team, project, ProjectName.From("Web"), LanguageCode.From("en")));

        CommandAssert.Succeeded(result);
        Assert.Equal(new ProjectAdded(project.Value, "Web", team.Value, "en"), _fixture.LastEvent<ProjectAdded>());
        Assert.Contains("language-code=en", _fixture.LastTags());
        Assert.Contains("project-name=web", _fixture.LastTags());
    }

    [Fact]
    public async Task AddProject_rejects_an_unknown_team()
    {
        var result = await _fixture.Handle(new AddProject(NewTeamId(), NewProjectId(), ProjectName.From("Web"), LanguageCode.From("en")));

        CommandAssert.Failed(result, CommandErrorKind.NotFound, "Team not found.");
    }

    [Fact]
    public async Task AddProject_rejects_a_taken_id()
    {
        var (team, project) = await _fixture.GivenProject();

        var result = await _fixture.Handle(new AddProject(team, project, ProjectName.From("Other"), LanguageCode.From("en")));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "A project with this ID already exists.");
    }

    [Fact]
    public async Task AddProject_compares_names_canonically()
    {
        var (team, _) = await _fixture.GivenProject();

        var result = await _fixture.Handle(new AddProject(team, NewProjectId(), ProjectName.From("WEB"), LanguageCode.From("en")));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "A project with this name already exists in this team.");
    }

    [Fact]
    public async Task AddLanguage_appends_the_event()
    {
        var (team, project) = await _fixture.GivenProject();

        var result = await _fixture.Handle(new AddLanguage(team, project, LanguageCode.From("de"), LanguageCode.From("en")));

        CommandAssert.Succeeded(result);
        Assert.Equal(new LanguageAdded("de", "en", project.Value), _fixture.LastEvent<LanguageAdded>());
    }

    [Fact]
    public async Task AddLanguage_accepts_an_added_language_as_parent()
    {
        var (team, project) = await _fixture.GivenProject();
        await _fixture.GivenLanguage(team, project, "de");

        var result = await _fixture.Handle(new AddLanguage(team, project, LanguageCode.From("de-AT"), LanguageCode.From("de")));

        CommandAssert.Succeeded(result);
    }

    [Fact]
    public async Task AddLanguage_rejects_a_project_outside_the_team()
    {
        var (team, _) = await _fixture.GivenProject();
        var (_, otherProject) = await _fixture.GivenProject();

        var result = await _fixture.Handle(new AddLanguage(team, otherProject, LanguageCode.From("de"), LanguageCode.From("en")));

        CommandAssert.Failed(result, CommandErrorKind.NotFound, "Project not found in this team.");
    }

    [Fact]
    public async Task AddLanguage_rejects_a_language_already_added()
    {
        var (team, project) = await _fixture.GivenProject();
        await _fixture.GivenLanguage(team, project, "de");

        var result = await _fixture.Handle(new AddLanguage(team, project, LanguageCode.From("de"), LanguageCode.From("en")));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "This language has already been added to the project.");
    }

    [Fact]
    public async Task AddLanguage_rejects_the_reference_language()
    {
        var (team, project) = await _fixture.GivenProject("en");

        var result = await _fixture.Handle(new AddLanguage(team, project, LanguageCode.From("en"), LanguageCode.From("en")));

        CommandAssert.Failed(result, CommandErrorKind.Invalid, "The reference language cannot be added as a new language.");
    }

    [Fact]
    public async Task AddLanguage_rejects_an_unknown_parent_language()
    {
        var (team, project) = await _fixture.GivenProject();

        var result = await _fixture.Handle(new AddLanguage(team, project, LanguageCode.From("fr"), LanguageCode.From("xx")));

        CommandAssert.Failed(result, CommandErrorKind.Invalid,
            "The parent language must be the project's reference language or a language already added to the project.");
    }
}
