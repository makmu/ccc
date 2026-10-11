using CCC.Domain.Events;
using CCC.EventStore;
using Dapper;
using Npgsql;

namespace CCC.Modules.TranslationDelivery.Queries;

sealed class TranslationsProjection : IProjection
{
    public string Schema => "translation_delivery";

    public int Version => 2;

    public IReadOnlyCollection<string> EventTypes { get; } =
    [
        nameof(ProjectAdded),
        nameof(LanguageAdded),
        nameof(KeyAdded),
        nameof(KeyRenamed),
        nameof(ReferenceTextProvided),
        nameof(ReferenceTextWithdrawn),
        nameof(TranslationProvided),
        nameof(TranslationWithdrawn),
    ];

    public Task InstallAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            CREATE TABLE IF NOT EXISTS translation_delivery.projects (
                project_id              UUID PRIMARY KEY,
                team_id                 UUID NOT NULL,
                reference_language_code TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS translation_delivery.languages (
                project_id           UUID NOT NULL,
                language_code        TEXT NOT NULL,
                parent_language_code TEXT,
                PRIMARY KEY (project_id, language_code)
            );

            CREATE TABLE IF NOT EXISTS translation_delivery.texts (
                project_id           UUID NOT NULL,
                language_code        TEXT NOT NULL,
                key_id               UUID NOT NULL,
                key_name             TEXT NOT NULL,
                text                 TEXT,
                source_language_code TEXT,
                PRIMARY KEY (project_id, language_code, key_id)
            );
            """, transaction: transaction, cancellationToken: cancellationToken));

    public Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        record.Type switch
        {
            nameof(ProjectAdded) => AddProjectAsync(record.PayloadAs<ProjectAdded>(), connection, transaction, cancellationToken),
            nameof(LanguageAdded) => AddLanguageAsync(record.PayloadAs<LanguageAdded>(), connection, transaction, cancellationToken),
            nameof(KeyAdded) => AddKeyAsync(record.PayloadAs<KeyAdded>(), connection, transaction, cancellationToken),
            nameof(KeyRenamed) => RenameKeyAsync(record.PayloadAs<KeyRenamed>(), connection, transaction, cancellationToken),
            nameof(ReferenceTextProvided) => ProvideReferenceTextAsync(record.PayloadAs<ReferenceTextProvided>(), connection, transaction, cancellationToken),
            nameof(ReferenceTextWithdrawn) => WithdrawReferenceTextAsync(record.PayloadAs<ReferenceTextWithdrawn>(), connection, transaction, cancellationToken),
            nameof(TranslationProvided) => ProvideTextAsync(record.PayloadAs<TranslationProvided>(), connection, transaction, cancellationToken),
            nameof(TranslationWithdrawn) => WithdrawTextAsync(record.PayloadAs<TranslationWithdrawn>(), connection, transaction, cancellationToken),
            _ => throw new InvalidOperationException($"Projection {Schema} does not handle {record.Type}."),
        };

    private static Task AddProjectAsync(ProjectAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO translation_delivery.projects (project_id, team_id, reference_language_code)
            VALUES (@Id, @TeamId, @ReferenceLanguage);

            INSERT INTO translation_delivery.languages (project_id, language_code, parent_language_code)
            VALUES (@Id, @ReferenceLanguage, NULL);
            """, e, transaction, cancellationToken: cancellationToken));

    private static Task AddLanguageAsync(LanguageAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO translation_delivery.languages (project_id, language_code, parent_language_code)
            VALUES (@ProjectId, @LanguageCode, @ParentLanguageCode);

            INSERT INTO translation_delivery.texts (project_id, language_code, key_id, key_name, text, source_language_code)
            SELECT project_id, @LanguageCode, key_id, key_name, text, source_language_code
            FROM translation_delivery.texts
            WHERE project_id = @ProjectId AND language_code = @ParentLanguageCode;
            """, e, transaction, cancellationToken: cancellationToken));

    private static Task AddKeyAsync(KeyAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO translation_delivery.texts (project_id, language_code, key_id, key_name)
            SELECT project_id, language_code, @Id, @Name
            FROM translation_delivery.languages
            WHERE project_id = @ProjectId;
            """, e, transaction, cancellationToken: cancellationToken));

    private static Task RenameKeyAsync(KeyRenamed e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            UPDATE translation_delivery.texts
            SET key_name = @Name
            WHERE project_id = @ProjectId AND key_id = @KeyId;
            """, e, transaction, cancellationToken: cancellationToken));

    private static async Task ProvideReferenceTextAsync(ReferenceTextProvided e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var referenceLanguageCode = await ReferenceLanguageCodeAsync(e.ProjectId, connection, transaction, cancellationToken);

        await ProvideTextAsync(new TranslationProvided(e.KeyId, e.ProjectId, referenceLanguageCode, e.Text), connection, transaction, cancellationToken);
    }

    private static Task ProvideTextAsync(TranslationProvided e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            UPDATE translation_delivery.texts
            SET text = @Text, source_language_code = @LanguageCode
            WHERE project_id = @ProjectId AND key_id = @KeyId AND language_code = @LanguageCode;

            WITH RECURSIVE
                inheriting AS (
                    SELECT l.language_code, l.parent_language_code
                    FROM translation_delivery.languages l
                    JOIN translation_delivery.texts t
                        ON t.project_id = l.project_id AND t.language_code = l.language_code AND t.key_id = @KeyId
                    WHERE l.project_id = @ProjectId
                      AND t.source_language_code IS DISTINCT FROM t.language_code
                ),
                heirs AS (
                    SELECT language_code FROM inheriting WHERE parent_language_code = @LanguageCode
                    UNION ALL
                    SELECT i.language_code FROM inheriting i JOIN heirs h ON i.parent_language_code = h.language_code
                )
            UPDATE translation_delivery.texts
            SET text = @Text, source_language_code = @LanguageCode
            WHERE project_id = @ProjectId AND key_id = @KeyId
              AND language_code IN (SELECT language_code FROM heirs);
            """, e, transaction, cancellationToken: cancellationToken));

    private static async Task WithdrawReferenceTextAsync(ReferenceTextWithdrawn e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var referenceLanguageCode = await ReferenceLanguageCodeAsync(e.ProjectId, connection, transaction, cancellationToken);

        await WithdrawTextAsync(new TranslationWithdrawn(e.KeyId, e.ProjectId, referenceLanguageCode), connection, transaction, cancellationToken);
    }

    private static Task WithdrawTextAsync(TranslationWithdrawn e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            WITH parent AS (
                SELECT p.text, p.source_language_code
                FROM translation_delivery.languages l
                JOIN translation_delivery.texts p
                    ON p.project_id = l.project_id AND p.language_code = l.parent_language_code AND p.key_id = @KeyId
                WHERE l.project_id = @ProjectId AND l.language_code = @LanguageCode
            )
            UPDATE translation_delivery.texts
            SET text = (SELECT text FROM parent), source_language_code = (SELECT source_language_code FROM parent)
            WHERE project_id = @ProjectId AND key_id = @KeyId AND source_language_code = @LanguageCode;
            """, e, transaction, cancellationToken: cancellationToken));

    private static async Task<string> ReferenceLanguageCodeAsync(Guid projectId, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        (await connection.ExecuteScalarAsync<string>(new CommandDefinition(
            "SELECT reference_language_code FROM translation_delivery.projects WHERE project_id = @projectId",
            new { projectId }, transaction, cancellationToken: cancellationToken)))!;
}
