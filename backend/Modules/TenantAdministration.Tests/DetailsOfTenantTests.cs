using CCC.Domain.Types;
using CCC.Modules.AppAdministration.Commands;
using CCC.Modules.TenantAdministration.Commands;
using CCC.Modules.TenantAdministration.Queries;
using CCC.TestKit;
using static CCC.TestKit.CommandFixture;

namespace CCC.Modules.TenantAdministration.Tests;

public class DetailsOfTenantTests
{
    private readonly CommandFixture _fixture = new();

    [Fact]
    public async Task An_unknown_tenant_has_no_details()
    {
        Assert.Null(await DetailsOf(NewTenantId()));
    }

    [Fact]
    public async Task A_tenant_shows_its_current_name_and_its_subscription_models_by_name()
    {
        var premium = NewSubscriptionModelId();
        var basic = NewSubscriptionModelId();
        await _fixture.Given(new AddSubscriptionModel(premium, SubscriptionModelName.From("Premium"), KeyLimit.From(1000)));
        await _fixture.Given(new AddSubscriptionModel(basic, SubscriptionModelName.From("Basic"), KeyLimit.From(10)));
        var tenant = NewTenantId();
        await _fixture.Given(new RegisterTenant(tenant, TenantName.From("Acme"), [premium, basic]));
        await _fixture.Given(new RenameTenant(tenant, TenantName.From("Acme Corp")));

        var details = await DetailsOf(tenant);

        Assert.NotNull(details);
        Assert.Equal("Acme Corp", details.Name);
        Assert.Equal(
            [new TenantSubscriptionModel(basic.Value, "Basic"), new TenantSubscriptionModel(premium.Value, "Premium")],
            details.SubscriptionModels);
    }

    private Task<TenantDetails?> DetailsOf(TenantId tenant) =>
        _fixture.Query<DetailsOfTenant, TenantDetails?>(new DetailsOfTenant(tenant));
}
