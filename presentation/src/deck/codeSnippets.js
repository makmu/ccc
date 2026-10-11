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
        var (name, alreadyNamed, nameTaken, context) =
            await loader.LoadAsync(
                Tenant.Name(command.TenantId),
                Tenant.IsNamed(command.TenantId, command.Name),
                Tenant.NameTaken(command.Name),
                ct);

        if (name is not { } currentName)
            return CommandResult.NotFound("Tenant not found.");

        if (alreadyNamed)
            return CommandResult.Ok;

        if (nameTaken)
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

export const stateLoaderSnippet = `public async Task<StateContext> LoadAsync(
    IReadOnlyList<IStateModel> models, CancellationToken ct)
{
    var filters = models.Select(m => m.Filter).ToArray();
    var union = new EventFilter(
        filters.SelectMany(f => f.Entries).ToList());
    var maxPosition = -1L;

    foreach (var record in
        await eventStore.LoadAsync(union, ct))
    {
        for (var i = 0; i < models.Count; i++)
        {
            if (filters[i].Matches(record))
                models[i].Apply(record);
        }

        if (record.Position > maxPosition)
            maxPosition = record.Position;
    }

    return new StateContext(eventStore, union, maxPosition);
}`

export const eventStoreSnippet = `public async Task AppendAsync(
    string eventType, object eventPayload, Guid user,
    long expectedMaxPosition, EventFilter contextFilter,
    string[] tags, CancellationToken ct)
{
    var json = JsonSerializer.Serialize(eventPayload);
    var (whereClause, parameters) =
        BuildFilterConditions(contextFilter);

    parameters.Add("type", eventType);
    parameters.Add("payload", json);
    parameters.Add("user", user);
    parameters.Add("tags", tags);
    parameters.Add(
        "expectedMaxPosition", expectedMaxPosition);

    var sql = $"""
        INSERT INTO events (type, payload, "user", tags)
        SELECT @type, @payload::jsonb, @user, @tags
        WHERE (
            SELECT COALESCE(MAX(position), -1)
            FROM events
            WHERE {whereClause}
        ) = @expectedMaxPosition
        """;

    var rows = await ExecuteUnderAppendLockAsync(
        sql, parameters, ct);

    if (rows == 0)
        throw new ConcurrencyException(
            "The event store was modified " +
            "by a concurrent operation.");
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

export const addKeyHandlerSnippet = `var (subscription, projectInTeam, idTaken,
     nameTaken, keysAdded, context) =
    await loader.LoadAsync(
        Team.Subscription(command.TeamId),
        Project.InTeam(command.TeamId, command.ProjectId),
        Key.IdTaken(command.Id),
        Key.NameTaken(command.ProjectId, command.Name),
        Team.KeysAdded(command.TeamId),
        ct);

if (subscription is not { } subscriptionModelId)
    return CommandResult.NotFound("Team not found.");

if (!projectInTeam)
    return CommandResult.NotFound(
        "Project not found in this team.");

if (idTaken)
    return CommandResult.Conflict("Key ID already taken.");

if (nameTaken)
    return CommandResult.Conflict("Key name already taken.");

var (keyLimit, _) = await loader.LoadAsync(
    Subscription.KeyLimit(subscriptionModelId), ct);

if (keysAdded >= keyLimit)
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
