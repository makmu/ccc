using System.Text.Json;
using CCC.Base.Abstractions;
using CCC.Base.ModuleHosting;
using CCC.Domain.Types;
using CCC.EventStore;
using CCC.Modules.AppAdministration.Commands;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace CCC.Base.TestKit;

// Runs an automation against a throwaway database. Its commands depend on tags, so
// tests set up state through commands, as with CommandFixture.
public sealed class AutomationFixture(string schema, Func<IServiceCollection, IServiceCollection> addModules)
{
    private static readonly TimeSpan CatchUpTimeout = TimeSpan.FromSeconds(5);

    private readonly TestDatabase _database = new();
    private ServiceProvider _services = null!;

    public async Task InitializeAsync()
    {
        await _database.CreateAsync();
        await StartAsync();
    }

    public async Task DisposeAsync()
    {
        await StopAsync();
        await _database.DropAsync();
    }

    public async Task RestartAsync()
    {
        await StopAsync();
        await StartAsync();
    }

    public async Task Given<TCommand>(TCommand command)
    {
        var result = await _services.GetRequiredService<ICommandHandler<TCommand>>().HandleAsync(command, CancellationToken.None);
        Assert.True(result.Succeeded, $"Setup command {typeof(TCommand).Name} failed: {result.Error}");
    }

    public async Task<(TenantId Tenant, SubscriptionModelId Subscription)> GivenTenant()
    {
        var subscription = CommandFixture.NewSubscriptionModelId();
        await Given(new AddSubscriptionModel(subscription, SubscriptionModelName.From("Basic"), KeyLimit.From(10)));
        var tenant = CommandFixture.NewTenantId();
        await Given(new RegisterTenant(tenant, TenantName.From($"Tenant {tenant}"), [subscription]));
        return (tenant, subscription);
    }

    public Task GivenUntagged<TEvent>(TEvent @event) =>
        _services.GetRequiredService<IEventStore>().AppendAsync(typeof(TEvent).Name, @event!, Guid.Empty, CancellationToken.None);

    public async Task<IReadOnlyList<TEvent>> Events<TEvent>()
    {
        await WaitForCatchUpAsync();
        var records = await _services.GetRequiredService<IEventStore>()
            .ReadAsync(0, int.MaxValue, [typeof(TEvent).Name], CancellationToken.None);
        return records.Select(record => JsonSerializer.Deserialize<TEvent>(record.Payload)!).ToList();
    }

    public async Task RewindAsync()
    {
        await using var connection = new NpgsqlConnection(_database.ConnectionString);
        await connection.ExecuteAsync($"UPDATE {schema}.bookmark SET position = 0");
    }

    public async Task<Bookmark> WaitForStallAsync()
    {
        var deadline = DateTime.UtcNow + CatchUpTimeout;
        while (true)
        {
            if (await HasBookmarkAsync() && await BookmarkAsync() is { StallReason: not null } bookmark)
                return bookmark;
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"The {schema} automation did not stall within {CatchUpTimeout}.");
            await Task.Delay(10);
        }
    }

    public async Task<HealthReportEntry> HealthAsync()
    {
        var report = await _services.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(registration => registration.Name == schema);
        return report.Entries[schema];
    }

    private async Task<bool> HasBookmarkAsync()
    {
        await using var connection = new NpgsqlConnection(_database.ConnectionString);
        return await connection.ExecuteScalarAsync<bool>($"SELECT to_regclass('{schema}.bookmark') IS NOT NULL");
    }

    private async Task<Bookmark> BookmarkAsync()
    {
        await using var connection = new NpgsqlConnection(_database.ConnectionString);
        return await connection.QuerySingleAsync<Bookmark>(
            $"SELECT position, stalled_at AS stalledAt, stall_reason AS stallReason FROM {schema}.bookmark");
    }

    private async Task WaitForCatchUpAsync()
    {
        var deadline = DateTime.UtcNow + CatchUpTimeout;
        while ((await HealthAsync()).Status != HealthStatus.Healthy)
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"The {schema} automation did not catch up within {CatchUpTimeout}.");
            await Task.Delay(10);
        }
    }

    private async Task StartAsync()
    {
        _services = addModules(new ServiceCollection()
                .AddLogging()
                .AddSingleton(new RunnerRetryPolicy(3, TimeSpan.FromMilliseconds(10)))
                .AddEventStore(_database.ConnectionString)
                .AddModuleDatabases(_ => _database.ConnectionString))
            .BuildServiceProvider();

        await EventStoreSchema.CreateAsync(_services);
        foreach (var service in _services.GetServices<IHostedService>())
            await service.StartAsync(CancellationToken.None);
    }

    private async Task StopAsync()
    {
        foreach (var service in _services.GetServices<IHostedService>())
            await service.StopAsync(CancellationToken.None);
        await _services.DisposeAsync();
    }
}
