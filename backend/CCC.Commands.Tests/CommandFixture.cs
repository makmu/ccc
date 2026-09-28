using System.Text.Json;
using CCC.Commands.Keys;
using CCC.Commands.Projects;
using CCC.Commands.SubscriptionModels;
using CCC.Commands.Tenants;
using CCC.Commands.Users;
using CCC.Domain.ValueObjects;
using CCC.EventStore;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.Commands.Tests;

// Resolves handlers the way the API does, over an in-memory event store.
// The Given* helpers build state by running real commands, so the setup
// carries exactly the tags the handlers write.
sealed class CommandFixture
{
    private readonly ServiceProvider _services;

    public CommandFixture()
    {
        _services = new ServiceCollection()
            .AddSingleton<IEventStore>(Store)
            .AddTransient<IStateLoader, StateLoader>()
            .AddCommandHandlers()
            .BuildServiceProvider();
    }

    public InMemoryEventStore Store { get; } = new();

    public Task<CommandResult> Handle<TCommand>(TCommand command) =>
        _services.GetRequiredService<ICommandHandler<TCommand>>().HandleAsync(command);

    public async Task Given<TCommand>(TCommand command)
    {
        var result = await Handle(command);
        Assert.True(result.Succeeded, $"Setup command {typeof(TCommand).Name} failed: {result.Error}");
    }

    public TEvent LastEvent<TEvent>()
    {
        var record = Store.Events[^1];
        Assert.Equal(typeof(TEvent).Name, record.Type);
        return JsonSerializer.Deserialize<TEvent>(record.Payload)!;
    }

    public string[] LastTags() => Store.Events[^1].Tags;

    public static TenantId NewTenantId() => TenantId.Create(Guid.NewGuid());
    public static TeamId NewTeamId() => TeamId.Create(Guid.NewGuid());
    public static ProjectId NewProjectId() => ProjectId.Create(Guid.NewGuid());
    public static KeyId NewKeyId() => KeyId.Create(Guid.NewGuid());
    public static UserId NewUserId() => UserId.Create(Guid.NewGuid());
    public static SubscriptionModelId NewSubscriptionModelId() => SubscriptionModelId.Create(Guid.NewGuid());

    public async Task<SubscriptionModelId> GivenSubscriptionModel(int keyLimit = 10)
    {
        var id = NewSubscriptionModelId();
        await Given(new AddSubscriptionModel(id, "Basic", KeyLimit.Create(keyLimit)));
        return id;
    }

    public async Task<UserId> GivenUser(string email = "alice@example.com")
    {
        var id = NewUserId();
        await Given(new AddUser(id, "Alice", EmailAddress.Create(email)));
        return id;
    }

    // Tenant names are unique across all tenants, so the default name is too.
    public async Task<(TenantId Tenant, SubscriptionModelId Subscription)> GivenTenant(string? name = null, int keyLimit = 10)
    {
        var subscription = await GivenSubscriptionModel(keyLimit);
        var tenant = NewTenantId();
        await Given(new RegisterTenant(tenant, TenantName.Create(name ?? $"Tenant {tenant}"), [subscription]));
        return (tenant, subscription);
    }

    public async Task<(TenantId Tenant, TeamId Team)> GivenTeam(int keyLimit = 10)
    {
        var (tenant, subscription) = await GivenTenant(keyLimit: keyLimit);
        var team = NewTeamId();
        await Given(new AddTeam(tenant, team, TeamName.Create("Team A"), subscription));
        return (tenant, team);
    }

    public async Task<(TeamId Team, ProjectId Project)> GivenProject(string referenceLanguage = "en", int keyLimit = 10)
    {
        var (_, team) = await GivenTeam(keyLimit);
        var project = NewProjectId();
        await Given(new AddProject(team, project, ProjectName.Create("Web"), LanguageCode.Create(referenceLanguage)));
        return (team, project);
    }

    public async Task<(TeamId Team, ProjectId Project, KeyId Key)> GivenKey(string name = "title", int keyLimit = 10)
    {
        var (team, project) = await GivenProject(keyLimit: keyLimit);
        var key = NewKeyId();
        await Given(new AddKey(team, project, key, KeyName.Create(name)));
        return (team, project, key);
    }

    public async Task GivenLanguage(TeamId team, ProjectId project, string code, string parent = "en") =>
        await Given(new AddLanguage(team, project, LanguageCode.Create(code), LanguageCode.Create(parent)));
}
