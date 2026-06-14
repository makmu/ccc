export const eventStoreSnippet = `public async Task AppendAsync(
    string eventType,
    object eventPayload,
    Guid user,
    long expectedMaxPosition,
    EventFilter contextFilter)
{
    var json = JsonSerializer.Serialize(eventPayload);
    var (whereClause, parameters) =
        BuildFilterConditions(contextFilter);

    parameters.Add("type", eventType);
    parameters.Add("payload", json);
    parameters.Add("user", user);
    parameters.Add(
        "expectedMaxPosition",
        expectedMaxPosition);

    var sql = $"""
        INSERT INTO events (type, payload, \"user\")
        SELECT @type, @payload::jsonb, @user
        WHERE (
            SELECT COALESCE(MAX(position), -1)
            FROM events
            WHERE {whereClause}
        ) = @expectedMaxPosition
        """;

    await using var conn =
        new NpgsqlConnection(connectionString);
    var rows = await conn.ExecuteAsync(sql, parameters);

    if (rows == 0)
    {
        throw new ConcurrencyException(
            "The event store was modified " +
            "by a concurrent operation.");
    }
}`

export const organizationRenameInsertSqlSnippet = `INSERT INTO events ("type", "payload", "user")
SELECT
    'OrganizationRenamed',
    '{ "organizationId": "org-42", "name": "Acme One" }'::jsonb,
    '7f4a0f2d-8b4a-4fcb-9a1d-3d2a6f7b8c91'
WHERE (
    SELECT COALESCE(MAX(position), -1)
    FROM events
    WHERE type = 'OrganizationAdded'
       OR type = 'OrganizationRenamed'
) = 42;`

export const organizationRenameEndpointSnippet = `app.MapPost("/organizations/{organizationId}/rename",
    async (Guid organizationId,
           RenameOrganizationRequest request,
           CommandContextBuilder contextBuilder) =>
{
    var context = await contextBuilder
        .Where<OrganizationAdded>()
        .Where<OrganizationRenamed>()
        .LoadAsync();

    var currentNames = new Dictionary<Guid, string>();
    foreach (var e in context.Events)
    {
        if (e.Type == nameof(OrganizationAdded))
        {
            var added = JsonSerializer
                .Deserialize<OrganizationAdded>(e.Payload)!;
            currentNames[added.Id] = added.Name;
        }
        else
        {
            var renamed = JsonSerializer
                .Deserialize<OrganizationRenamed>(e.Payload)!;
            currentNames[renamed.OrganizationId] = renamed.Name;
        }
    }

    if (!currentNames.ContainsKey(organizationId))
        return Results.NotFound("Organization not found.");

    if (currentNames.Any(
        o => o.Key != organizationId && o.Value == request.Name))
    {
        return Results.Conflict(
            "An organization with this name already exists.");
    }

    try
    {
        await context.AppendAsync(
            new OrganizationRenamed(organizationId, request.Name));
    }
    catch (ConcurrencyException)
    {
        return Results.Conflict(
            "A concurrent operation modified the event store. " +
            "Please retry.");
    }

    return Results.Ok();
});`

export const keyEndpointSnippet = `app.MapPost(
    "/teams/{teamId}/projects/{projectId}/keys",
    async (
        Guid teamId,
        Guid projectId,
        AddKeyRequest request,
        CommandContextBuilder contextBuilder) =>
{
    var context = await contextBuilder
        .Where<TeamAdded>(w => w.With(e => e.Id, teamId))
        .Where<ProjectAdded>(w => w.With(e => e.TeamId, teamId))
        .Where<SubscriptionModelAdded>()
        .Where<KeyAdded>()
        .Where<KeyRenamed>(
            w => w.With(e => e.ProjectId, projectId))
        .LoadAsync();

    // ... rebuild project, team, subscription model,
    // ... and current key state

    if (keys.Count >= subscriptionModel.KeyLimit)
        return Results.Conflict(
            "The key limit for this team's subscription model " +
            "has been reached.");

    if (CurrentKeyNames(
        keys.Where(k => k.ProjectId == projectId),
        keyRenames).Contains(request.Name))
    {
        return Results.Conflict(
            "A key with this name already exists in this project.");
    }

    // ... append event
});`
