using CCC.Base.TestKit;
using CCC.Domain.Events;
using CCC.Modules.AppAdministration.Queries;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CCC.Modules.AppAdministration.Tests;

public class ProjectionHealthTests : IAsyncLifetime
{
    private readonly ProjectionFixture _fixture = new("app_administration", services => services.AddAppAdministration());

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task A_caught_up_projection_is_healthy()
    {
        await _fixture.Given(new UserAdded(Guid.NewGuid(), "Carol", "carol@example.com"));
        await _fixture.Query<Users, IReadOnlyList<UserSummary>>(new Users());

        var health = await _fixture.HealthAsync();

        Assert.Equal(HealthStatus.Healthy, health.Status);
        Assert.Equal(["version", "position"], health.Data.Keys);
    }

    [Fact]
    public async Task A_projection_whose_version_is_not_installed_is_unhealthy()
    {
        await _fixture.Query<Users, IReadOnlyList<UserSummary>>(new Users());
        await _fixture.GivenInstalledVersion(99);

        var health = await _fixture.HealthAsync();

        Assert.Equal(HealthStatus.Unhealthy, health.Status);
        Assert.Equal("Version 1 is not installed, so its queries answer from version 99.", health.Description);
        Assert.Equal(99, health.Data["version"]);
    }

    [Fact]
    public async Task A_stalled_projection_is_unhealthy_and_tells_why()
    {
        var user = Guid.NewGuid();
        await _fixture.Given(new UserAdded(user, "Carol", "carol@example.com"));
        await _fixture.Given(new UserAdded(user, "Carol", "carol@example.com"));
        var stalled = await _fixture.WaitForStallAsync();

        var health = await _fixture.HealthAsync();

        Assert.Equal(HealthStatus.Unhealthy, health.Status);
        Assert.Contains(stalled.StallReason!, health.Description);
    }
}
