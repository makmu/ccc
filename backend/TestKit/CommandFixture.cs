using System.Text.Json;
using CCC.Abstractions;
using CCC.Commands;
using CCC.Commands.Keys;
using CCC.Commands.Projects;
using CCC.Commands.SubscriptionModels;
using CCC.Commands.Tenants;
using CCC.Commands.Users;
using CCC.Domain.Types;
using CCC.EventStore;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.TestKit;

public sealed class CommandFixture
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

    public static TenantId NewTenantId() => TenantId.From(Guid.NewGuid());
    public static TeamId NewTeamId() => TeamId.From(Guid.NewGuid());
    public static ProjectId NewProjectId() => ProjectId.From(Guid.NewGuid());
    public static KeyId NewKeyId() => KeyId.From(Guid.NewGuid());
    public static UserId NewUserId() => UserId.From(Guid.NewGuid());
    public static SubscriptionModelId NewSubscriptionModelId() => SubscriptionModelId.From(Guid.NewGuid());

    public async Task<SubscriptionModelId> GivenSubscriptionModel(int keyLimit = 10)
    {
        var id = NewSubscriptionModelId();
        await Given(new AddSubscriptionModel(id, "Basic", KeyLimit.From(keyLimit)));
        return id;
    }

    public async Task<UserId> GivenUser(string email = "alice@example.com")
    {
        var id = NewUserId();
        await Given(new AddUser(id, "Alice", EmailAddress.From(email)));
        return id;
    }

    public async Task<(TenantId Tenant, SubscriptionModelId Subscription)> GivenTenant(string? name = null, int keyLimit = 10)
    {
        var subscription = await GivenSubscriptionModel(keyLimit);
        var tenant = NewTenantId();
        await Given(new RegisterTenant(tenant, TenantName.From(name ?? $"Tenant {tenant}"), [subscription]));
        return (tenant, subscription);
    }

    public async Task<(TenantId Tenant, TeamId Team)> GivenTeam(int keyLimit = 10)
    {
        var (tenant, subscription) = await GivenTenant(keyLimit: keyLimit);
        var team = NewTeamId();
        await Given(new AddTeam(tenant, team, TeamName.From("Team A"), subscription));
        return (tenant, team);
    }

    public async Task<(TeamId Team, ProjectId Project)> GivenProject(string referenceLanguage = "en", int keyLimit = 10)
    {
        var (_, team) = await GivenTeam(keyLimit);
        var project = NewProjectId();
        await Given(new AddProject(team, project, ProjectName.From("Web"), LanguageCode.From(referenceLanguage)));
        return (team, project);
    }

    public async Task<(TeamId Team, ProjectId Project, KeyId Key)> GivenKey(string name = "title", int keyLimit = 10)
    {
        var (team, project) = await GivenProject(keyLimit: keyLimit);
        var key = NewKeyId();
        await Given(new AddKey(team, project, key, KeyName.From(name)));
        return (team, project, key);
    }

    public async Task GivenLanguage(TeamId team, ProjectId project, string code, string parent = "en") =>
        await Given(new AddLanguage(team, project, LanguageCode.From(code), LanguageCode.From(parent)));
}
