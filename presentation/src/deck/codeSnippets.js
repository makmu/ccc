export const stateModelSnippet = `public static class Tenant
{
    public static StateModel<TenantName?> Name(
        TenantId tenantId) =>
        StateModel.Init<TenantName?>(null)
            .Where(Tag.TenantId(tenantId))
            .When<TenantRegistered>(
                (e, _) => TenantName.From(e.Name))
            .When<TenantRenamed>(
                (e, _) => TenantName.From(e.Name));

    public static StateModel<bool> NameTaken(
        TenantName name) =>
        StateModel.Init(false)
            .Where(Tag.TenantName(name))
            .When<TenantRegistered>((_, _) => true)
            .When<TenantRenamed>((e, _) =>
                CanonicalForm.Of(e.Name) == name.Canonical);
}`

export const tenantRenameHandlerSnippet = `sealed class RenameTenantHandler(IStateLoader loader)
    : ICommandHandler<RenameTenant>
{
    public async Task<CommandResult> HandleAsync(
        RenameTenant command, CancellationToken ct)
    {
        var name = Tenant.Name(command.TenantId);
        var alreadyNamed =
            Tenant.IsNamed(command.TenantId, command.Name);
        var nameTaken = Tenant.NameTaken(command.Name);
        var context = await loader.LoadAsync(
            [name, alreadyNamed, nameTaken], ct);

        if (name.IsNull(out var currentName))
            return CommandResult.NotFound("Tenant not found.");

        if (alreadyNamed.IsTrue())
            return CommandResult.Ok;

        if (nameTaken.IsTrue())
            return CommandResult.Conflict(
                "A tenant with this name already exists.");

        var appended = await context.TryAppendAsync(
            new TenantRenamed(
                command.TenantId.Value, command.Name.Value),
            Tags.From(
                Tag.TenantId(command.TenantId),
                Tag.TenantName(currentName),
                Tag.TenantName(command.Name)),
            ct);

        return appended
            ? CommandResult.Ok
            : CommandResult.ConcurrentModification;
    }
}`

export const tenantRenameInsertSqlSnippet = `BEGIN; -- READ COMMITTED
SELECT pg_advisory_xact_lock(@AppendLockKey);

INSERT INTO events (type, payload, "user", tags)
SELECT
    'TenantRenamed',
    '{ "TenantId": "42", "Name": "Horizon" }'::jsonb,
    '00000000-0000-0000-0000-000000000000',
    '{tenant-id=42,tenant-name=acme,tenant-name=horizon}'
WHERE (
    SELECT COALESCE(MAX(position), -1)
    FROM events
    WHERE (type = 'TenantRegistered'
           AND tags @> '{tenant-id=42}')
       OR (type = 'TenantRenamed'
           AND tags @> '{tenant-id=42}')
       OR (type = 'TenantRegistered'
           AND tags @> '{tenant-name=horizon}')
       OR (type = 'TenantRenamed'
           AND tags @> '{tenant-name=horizon}')
) = 40;

COMMIT;`

export const addKeyHandlerSnippet = `var subscription = Team.Subscription(command.TeamId);
var projectInTeam =
    Project.InTeam(command.TeamId, command.ProjectId);
var idTaken = Key.IdTaken(command.Id);
var nameTaken =
    Key.NameTaken(command.ProjectId, command.Name);
var keysAdded = Team.KeysAdded(command.TeamId);
var context = await loader.LoadAsync(
    [subscription, projectInTeam, idTaken,
     nameTaken, keysAdded], ct);

if (subscription.IsNull(out var subscriptionModelId))
    return CommandResult.NotFound("Team not found.");

if (projectInTeam.IsFalse())
    return CommandResult.NotFound(
        "Project not found in this team.");

if (idTaken.IsTrue())
    return CommandResult.Conflict("Key ID already taken.");

if (nameTaken.IsTrue())
    return CommandResult.Conflict("Key name already taken.");

var keyLimit = Subscription.KeyLimit(subscriptionModelId);
await loader.LoadAsync([keyLimit], ct);

if (keysAdded.Value >= keyLimit.Value)
    return CommandResult.Conflict(
        "The key limit for this team's subscription " +
        "model has been reached.");

var appended = await context.TryAppendAsync(
    new KeyAdded(command.Id.Value,
        command.Name.Value, command.ProjectId.Value),
    Tags.From(
        Tag.TeamId(command.TeamId),
        Tag.ProjectId(command.ProjectId),
        Tag.KeyId(command.Id),
        Tag.KeyName(command.Name)),
    ct);`

export const projectionSnippet = `sealed class TranslationsProjection : IProjection
{
    public string Schema => "translation_delivery";

    public int Version => 2;

    public EventHandlers<CommandDefinition> Handlers { get; } =
        new EventHandlers<CommandDefinition>()
            .On<ProjectAdded>(AddProject)
            .On<LanguageAdded>(AddLanguage)
            .On<KeyAdded>(AddKey)
            .On<KeyRenamed>(RenameKey)
            .On<TranslationProvided>(ProvideTranslation);

    // ... InstallAsync, ApplyAsync and more events

    private static CommandDefinition RenameKey(KeyRenamed e) =>
        new("""
            UPDATE translation_delivery.texts
            SET key_name = @Name
            WHERE project_id = @ProjectId
              AND key_id = @KeyId;
            """, e);
}`
