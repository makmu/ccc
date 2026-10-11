using CCC.Domain.Events;
using CCC.TestKit;

namespace CCC.Modules.AppAdministration.Tests;

public class ProjectionStallTests : IAsyncLifetime
{
    private readonly ProjectionFixture _fixture = new("app_administration", services => services.AddAppAdministration());

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task A_projection_that_keeps_failing_stalls_and_records_why()
    {
        var user = Guid.NewGuid();
        await _fixture.Given(new UserAdded(user, "Carol", "carol@example.com"));
        await _fixture.Given(new UserAdded(user, "Carol", "carol@example.com"));

        var bookmark = await _fixture.WaitForStallAsync();

        Assert.Matches(@"^Applying event \d+ \(UserAdded\) failed: 23505", bookmark.StallReason);
    }

    [Fact]
    public async Task A_stalled_projection_does_not_catch_up_after_a_restart()
    {
        var user = Guid.NewGuid();
        await _fixture.Given(new UserAdded(user, "Carol", "carol@example.com"));
        await _fixture.Given(new UserAdded(user, "Carol", "carol@example.com"));
        var stalled = await _fixture.WaitForStallAsync();

        await _fixture.RestartAsync();
        await _fixture.Given(new UserAdded(Guid.NewGuid(), "Dave", "dave@example.com"));
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        Assert.Equal(stalled, await _fixture.BookmarkAsync());
    }
}
