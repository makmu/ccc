using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Base.TestKit;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.Modules.AppAdministration.Commands;
using CCC.Modules.TenantAdministration.Commands;
using static CCC.Base.TestKit.CommandFixture;

namespace CCC.Modules.TenantAdministration.Tests;

public class DefaultTeamTests
{
    private readonly CommandFixture _fixture = new();

    [Fact]
    public async Task AddDefaultTeam_adds_a_team_named_Default_with_the_first_subscription_model_of_the_tenant()
    {
        var first = await _fixture.GivenSubscriptionModel();
        var second = await _fixture.GivenSubscriptionModel();
        var tenant = NewTenantId();
        await _fixture.Given(new RegisterTenant(tenant, TenantName.From("Acme"), [first, second]));
        var team = NewTeamId();

        var result = await _fixture.Handle(new AddDefaultTeam(tenant, team));

        CommandAssert.Succeeded(result);
        Assert.Equal(new TeamAdded(team.Value, "Default", tenant.Value, first.Value), _fixture.LastEvent<TeamAdded>());
        Assert.Equal([$"team-id={team}", "team-name=default", $"tenant-id={tenant}"], _fixture.LastTags());
    }

    [Fact]
    public async Task AddDefaultTeam_appends_nothing_when_the_tenant_already_has_a_team()
    {
        var (tenant, _) = await _fixture.GivenTenant();
        await _fixture.Given(new AddDefaultTeam(tenant, NewTeamId()));
        var eventsBefore = _fixture.Store.Events.Count;

        var result = await _fixture.Handle(new AddDefaultTeam(tenant, NewTeamId()));

        CommandAssert.Succeeded(result);
        Assert.Equal(eventsBefore, _fixture.Store.Events.Count);
    }

    [Fact]
    public async Task AddDefaultTeam_rejects_an_unknown_tenant()
    {
        var result = await _fixture.Handle(new AddDefaultTeam(NewTenantId(), NewTeamId()));

        CommandAssert.Failed(result, CommandErrorKind.NotFound, "Tenant not found.");
    }

    [Fact]
    public async Task AddDefaultTeam_conflicts_with_a_team_added_to_the_tenant_between_load_and_append()
    {
        var (tenant, subscription) = await _fixture.GivenTenant();
        var competingTeam = NewTeamId();

        _fixture.Store.BeforeConditionalAppend = () =>
        {
            _fixture.Store.BeforeConditionalAppend = null;
            _fixture.Store.Append(new TeamAdded(competingTeam.Value, "Team A", tenant.Value, subscription.Value),
                Tags.From(Tag.TeamId(competingTeam), Tag.TenantId(tenant), Tag.TeamName(TeamName.From("Team A"))));
        };

        var result = await _fixture.Handle(new AddDefaultTeam(tenant, NewTeamId()));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "A concurrent operation modified the event store. Please retry.");
        Assert.Equal(competingTeam.Value, _fixture.LastEvent<TeamAdded>().Id);
    }
}
