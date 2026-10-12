using CCC.Base.TestKit;
using CCC.Domain.Events;
using CCC.Modules.AppAdministration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CCC.Modules.TenantAdministration.Tests;

public class DefaultTeamAutomationTests : IAsyncLifetime
{
    private readonly AutomationFixture _fixture = new("default_team_automation",
        services => services.AddAppAdministration().AddTenantAdministration());

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task A_registered_tenant_gets_a_default_team()
    {
        var (tenant, subscription) = await _fixture.GivenTenant();

        var team = Assert.Single(await _fixture.Events<TeamAdded>());

        Assert.Equal(("Default", tenant.Value, subscription.Value), (team.Name, team.TenantId, team.SubscriptionModelId));
    }

    [Fact]
    public async Task A_caught_up_automation_is_healthy_and_reports_no_version()
    {
        await _fixture.GivenTenant();
        await _fixture.Events<TeamAdded>();

        var health = await _fixture.HealthAsync();

        Assert.Equal(HealthStatus.Healthy, health.Status);
        Assert.Equal(["position"], health.Data.Keys);
    }

    [Fact]
    public async Task Reacting_to_the_same_event_again_adds_no_second_team()
    {
        await _fixture.GivenTenant();
        var first = Assert.Single(await _fixture.Events<TeamAdded>());

        await _fixture.RewindAsync();
        await _fixture.RestartAsync();

        Assert.Equal(first, Assert.Single(await _fixture.Events<TeamAdded>()));
    }

    [Fact]
    public async Task An_event_the_automation_cannot_react_to_stalls_it_and_tells_why()
    {
        await _fixture.GivenUntagged(new TenantRegistered(Guid.NewGuid(), "Acme", [Guid.NewGuid()]));

        var stalled = await _fixture.WaitForStallAsync();
        var health = await _fixture.HealthAsync();

        Assert.Equal("Reacting to event 1 (TenantRegistered) failed: NotFound: Tenant not found.", stalled.StallReason);
        Assert.Equal(HealthStatus.Unhealthy, health.Status);
        Assert.Contains(stalled.StallReason!, health.Description);
    }
}
