using CCC.Domain.Events;
using CCC.Modules.AppAdministration.Queries;
using CCC.TestKit;

namespace CCC.Modules.AppAdministration.Tests;

public class ProjectionTests : IAsyncLifetime
{
    private readonly ProjectionFixture _fixture = new("app_administration", services => services.AddAppAdministration());
    private readonly Guid _basic = Guid.NewGuid();
    private readonly Guid _premium = Guid.NewGuid();
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        await _fixture.Given(new SubscriptionModelAdded(_premium, "Premium", 1000));
        await _fixture.Given(new SubscriptionModelAdded(_basic, "Basic", 10));
        await _fixture.Given(new UserAdded(_bob, "Bob", "bob@example.com"));
        await _fixture.Given(new UserAdded(_alice, "Alice", "alice@example.com"));
    }

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Subscription_models_are_listed_by_name_with_their_key_limits()
    {
        var subscriptionModels = await _fixture.Query<SubscriptionModels, IReadOnlyList<SubscriptionModelSummary>>(new SubscriptionModels());

        Assert.Equal(
            [new SubscriptionModelSummary(_basic, "Basic", 10), new SubscriptionModelSummary(_premium, "Premium", 1000)],
            subscriptionModels);
    }

    [Fact]
    public async Task Users_are_listed_by_name()
    {
        var users = await _fixture.Query<Users, IReadOnlyList<UserSummary>>(new Users());

        Assert.Equal(
            [new UserSummary(_alice, "Alice", "alice@example.com"), new UserSummary(_bob, "Bob", "bob@example.com")],
            users);
    }

    [Fact]
    public async Task A_tenant_is_listed_with_its_subscription_models_and_owners()
    {
        var tenant = Guid.NewGuid();
        await _fixture.Given(new TenantRegistered(tenant, "Acme", [_premium, _basic]));
        await _fixture.Given(new TenantOwnerAssigned(_bob, tenant));
        await _fixture.Given(new TenantOwnerAssigned(_alice, tenant));

        var listed = Assert.Single(await Tenants());

        Assert.Equal(tenant, listed.Id);
        Assert.Equal("Acme", listed.Name);
        Assert.Equal(["Basic", "Premium"], listed.SubscriptionModels);
        Assert.Equal(["Alice", "Bob"], listed.Owners);
    }

    [Fact]
    public async Task A_tenant_without_owners_is_listed_with_none()
    {
        await _fixture.Given(new TenantRegistered(Guid.NewGuid(), "Acme", [_basic]));

        Assert.Empty(Assert.Single(await Tenants()).Owners);
    }

    [Fact]
    public async Task Tenants_are_listed_by_their_current_name()
    {
        var acme = Guid.NewGuid();
        await _fixture.Given(new TenantRegistered(acme, "Acme", [_basic]));
        await _fixture.Given(new TenantRegistered(Guid.NewGuid(), "Bolt", [_basic]));
        await _fixture.Given(new TenantRenamed(acme, "Zenith"));

        Assert.Equal(["Bolt", "Zenith"], (await Tenants()).Select(t => t.Name));
    }

    private Task<IReadOnlyList<TenantSummary>> Tenants() =>
        _fixture.Query<Tenants, IReadOnlyList<TenantSummary>>(new Tenants());
}
