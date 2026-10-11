using CCC.Base.TestKit;
using CCC.Domain.Types;
using CCC.Modules.TranslationDelivery.Queries;
using static CCC.Base.TestKit.CommandFixture;

namespace CCC.Modules.TranslationDelivery.Tests;

public class LanguagesOfProjectTests
{
    private readonly CommandFixture _fixture = new();

    [Fact]
    public async Task A_project_of_another_team_has_no_languages_to_show()
    {
        var (_, project) = await _fixture.GivenProject();

        Assert.Null(await LanguagesOf(NewTeamId(), project));
    }

    [Fact]
    public async Task The_reference_language_comes_first_followed_by_the_languages_in_the_order_they_were_added()
    {
        var (team, project) = await _fixture.GivenProject("en");
        await _fixture.GivenLanguage(team, project, "fr");
        await _fixture.GivenLanguage(team, project, "de");
        await _fixture.GivenLanguage(team, project, "de-AT", parent: "de");

        Assert.Equal(["en", "fr", "de", "de-AT"], await LanguagesOf(team, project));
    }

    private Task<IReadOnlyList<string>?> LanguagesOf(TeamId team, ProjectId project) =>
        _fixture.Query<LanguagesOfProject, IReadOnlyList<string>?>(new LanguagesOfProject(team, project));
}
