using CCC.Abstractions;
using CCC.Domain.Events;
using CCC.Domain.Types;
using CCC.Modules.AppAdministration.Commands;
using CCC.Modules.TenantAdministration.Commands;
using CCC.TestKit;
using static CCC.TestKit.CommandFixture;

namespace CCC.Modules.TenantAdministration.Tests;

public class TenantTests
{
    private readonly CommandFixture _fixture = new();

    [Fact]
    public async Task RenameTenant_rejects_an_unknown_tenant()
    {
        var result = await _fixture.Handle(new RenameTenant(NewTenantId(), TenantName.From("Acme")));

        CommandAssert.Failed(result, CommandErrorKind.NotFound, "Tenant not found.");
    }

    [Fact]
    public async Task RenameTenant_to_the_same_name_in_other_casing_appends_nothing()
    {
        var (tenant, _) = await _fixture.GivenTenant("Acme");
        var eventsBefore = _fixture.Store.Events.Count;

        var result = await _fixture.Handle(new RenameTenant(tenant, TenantName.From("ACME")));

        CommandAssert.Succeeded(result);
        Assert.Equal(eventsBefore, _fixture.Store.Events.Count);
    }

    [Fact]
    public async Task RenameTenant_rejects_a_taken_name()
    {
        var (tenant, _) = await _fixture.GivenTenant("Acme");
        await _fixture.GivenTenant("Globex");

        var result = await _fixture.Handle(new RenameTenant(tenant, TenantName.From("globex")));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "A tenant with this name already exists.");
    }

    [Fact]
    public async Task RenameTenant_tags_both_names_and_frees_the_old_one()
    {
        var (tenant, subscription) = await _fixture.GivenTenant("Acme");

        var result = await _fixture.Handle(new RenameTenant(tenant, TenantName.From("Acme Corp")));

        CommandAssert.Succeeded(result);
        Assert.Equal(new TenantRenamed(tenant.Value, "Acme Corp"), _fixture.LastEvent<TenantRenamed>());
        Assert.Equal([$"tenant-id={tenant}", "tenant-name=acme", "tenant-name=acme corp"], _fixture.LastTags());

        var reuse = await _fixture.Handle(new RegisterTenant(NewTenantId(), TenantName.From("Acme"), [subscription]));
        CommandAssert.Succeeded(reuse);
    }

    [Fact]
    public async Task AddTeam_appends_the_event()
    {
        var (tenant, subscription) = await _fixture.GivenTenant();
        var team = NewTeamId();

        var result = await _fixture.Handle(new AddTeam(tenant, team, TeamName.From("Team A"), subscription));

        CommandAssert.Succeeded(result);
        Assert.Equal(new TeamAdded(team.Value, "Team A", tenant.Value, subscription.Value), _fixture.LastEvent<TeamAdded>());
    }

    [Fact]
    public async Task AddTeam_rejects_an_unknown_tenant()
    {
        var subscription = await _fixture.GivenSubscriptionModel();

        var result = await _fixture.Handle(new AddTeam(NewTenantId(), NewTeamId(), TeamName.From("Team A"), subscription));

        CommandAssert.Failed(result, CommandErrorKind.NotFound, "Tenant not found.");
    }

    [Fact]
    public async Task AddTeam_rejects_a_subscription_model_the_tenant_does_not_have()
    {
        var (tenant, _) = await _fixture.GivenTenant();
        var otherSubscription = await _fixture.GivenSubscriptionModel();

        var result = await _fixture.Handle(new AddTeam(tenant, NewTeamId(), TeamName.From("Team A"), otherSubscription));

        CommandAssert.Failed(result, CommandErrorKind.Invalid, "The specified subscription model is not available in this tenant.");
    }

    [Fact]
    public async Task AddTeam_compares_names_canonically()
    {
        var (tenant, subscription) = await _fixture.GivenTenant();
        await _fixture.Given(new AddTeam(tenant, NewTeamId(), TeamName.From("Team A"), subscription));

        var result = await _fixture.Handle(new AddTeam(tenant, NewTeamId(), TeamName.From("team a"), subscription));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "A team with this id or name already exists in this tenant.");
    }
}
