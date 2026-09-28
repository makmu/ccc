using CCC.Commands.Tenants;
using CCC.Domain.Events;
using CCC.Domain.Types;
using static CCC.Commands.Tests.CommandFixture;

namespace CCC.Commands.Tests;

public class TenantTests
{
    private readonly CommandFixture _fixture = new();

    [Fact]
    public async Task RegisterTenant_appends_the_event_tagged_with_the_canonical_name()
    {
        var subscription = await _fixture.GivenSubscriptionModel();
        var id = NewTenantId();

        var result = await _fixture.Handle(new RegisterTenant(id, TenantName.From(" Acme "), [subscription]));

        CommandAssert.Succeeded(result);
        var registered = _fixture.LastEvent<TenantRegistered>();
        Assert.Equal(" Acme ", registered.Name);
        Assert.Equal([subscription.Value], registered.SubscriptionModelIds);
        Assert.Equal([$"tenant-id={id}", "tenant-name=acme"], _fixture.LastTags());
    }

    [Fact]
    public async Task RegisterTenant_requires_a_subscription_model_before_loading_anything()
    {
        var result = await _fixture.Handle(new RegisterTenant(NewTenantId(), TenantName.From("Acme"), []));

        CommandAssert.Failed(result, CommandErrorKind.Invalid, "At least one subscription model must be referenced.");
        Assert.Empty(_fixture.Store.Events);
    }

    [Fact]
    public async Task RegisterTenant_rejects_a_taken_id()
    {
        var (tenant, subscription) = await _fixture.GivenTenant("Acme");

        var result = await _fixture.Handle(new RegisterTenant(tenant, TenantName.From("Other"), [subscription]));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "A tenant with this id already exists.");
    }

    [Fact]
    public async Task RegisterTenant_compares_names_canonically()
    {
        var (_, subscription) = await _fixture.GivenTenant("Acme");

        var result = await _fixture.Handle(new RegisterTenant(NewTenantId(), TenantName.From(" ACME "), [subscription]));

        CommandAssert.Failed(result, CommandErrorKind.Conflict, "A tenant with this name already exists.");
    }

    [Fact]
    public async Task RegisterTenant_accepts_several_known_subscription_models()
    {
        var first = await _fixture.GivenSubscriptionModel();
        var second = await _fixture.GivenSubscriptionModel();

        var result = await _fixture.Handle(new RegisterTenant(NewTenantId(), TenantName.From("Acme"), [first, second]));

        CommandAssert.Succeeded(result);
        Assert.Equal([first.Value, second.Value], _fixture.LastEvent<TenantRegistered>().SubscriptionModelIds);
    }

    [Fact]
    public async Task RegisterTenant_rejects_an_unknown_subscription_model()
    {
        var known = await _fixture.GivenSubscriptionModel();

        var result = await _fixture.Handle(new RegisterTenant(NewTenantId(), TenantName.From("Acme"), [known, NewSubscriptionModelId()]));

        CommandAssert.Failed(result, CommandErrorKind.NotFound, "One or more subscription models were not found.");
    }

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

    [Fact]
    public async Task AssignTenantOwner_appends_the_event()
    {
        var (tenant, _) = await _fixture.GivenTenant();
        var user = await _fixture.GivenUser();

        var result = await _fixture.Handle(new AssignTenantOwner(tenant, user));

        CommandAssert.Succeeded(result);
        Assert.Equal(new TenantOwnerAssigned(user.Value, tenant.Value), _fixture.LastEvent<TenantOwnerAssigned>());
    }

    [Fact]
    public async Task AssignTenantOwner_checks_tenant_then_user_then_existing_assignment()
    {
        var (tenant, _) = await _fixture.GivenTenant();
        var user = await _fixture.GivenUser();

        CommandAssert.Failed(await _fixture.Handle(new AssignTenantOwner(NewTenantId(), NewUserId())),
            CommandErrorKind.NotFound, "Tenant not found.");
        CommandAssert.Failed(await _fixture.Handle(new AssignTenantOwner(tenant, NewUserId())),
            CommandErrorKind.NotFound, "User not found.");

        await _fixture.Given(new AssignTenantOwner(tenant, user));
        CommandAssert.Failed(await _fixture.Handle(new AssignTenantOwner(tenant, user)),
            CommandErrorKind.Conflict, "This user is already an owner of this tenant.");
    }
}
