using CCC.Domain.Events;
using CCC.Domain.Types;
using CCC.TestKit;

namespace CCC.Modules.TenantAdministration.Tests;

public class ReadModelTests : IAsyncLifetime
{
    private readonly ReadModelFixture _fixture = new("tenant_administration", services => services.AddTenantAdministration());
    private readonly Guid _tenant = Guid.NewGuid();
    private readonly Guid _basic = Guid.NewGuid();
    private readonly Guid _premium = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        await _fixture.Given(new SubscriptionModelAdded(_basic, "Basic", 10));
        await _fixture.Given(new SubscriptionModelAdded(_premium, "Premium", 1000));
    }

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task A_tenant_without_teams_lists_none()
    {
        Assert.Empty(await TeamsOf(_tenant));
    }

    [Fact]
    public async Task Teams_are_listed_by_name_with_their_subscription_model()
    {
        var web = Guid.NewGuid();
        var apps = Guid.NewGuid();
        await _fixture.Given(new TeamAdded(web, "Web", _tenant, _premium));
        await _fixture.Given(new TeamAdded(apps, "Apps", _tenant, _basic));

        Assert.Equal(
            [new TeamSummary(apps, "Apps", "Basic"), new TeamSummary(web, "Web", "Premium")],
            await TeamsOf(_tenant));
    }

    [Fact]
    public async Task Only_the_tenants_own_teams_are_listed()
    {
        await _fixture.Given(new TeamAdded(Guid.NewGuid(), "Web", Guid.NewGuid(), _basic));

        Assert.Empty(await TeamsOf(_tenant));
    }

    private Task<IReadOnlyList<TeamSummary>> TeamsOf(Guid tenant) =>
        _fixture.Query<TeamsOfTenant, IReadOnlyList<TeamSummary>>(new TeamsOfTenant(TenantId.From(tenant)));
}
