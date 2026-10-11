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

    public Task InstallAsync(NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
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
            """, transaction: transaction);

    public Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        record.Type switch
        {
            nameof(ProjectAdded) => AddProjectAsync(record.PayloadAs<ProjectAdded>(), connection, transaction),
            nameof(LanguageAdded) => AddLanguageAsync(record.PayloadAs<LanguageAdded>(), connection, transaction),
            nameof(KeyAdded) => AddKeyAsync(record.PayloadAs<KeyAdded>(), connection, transaction),
            nameof(KeyRenamed) => RenameKeyAsync(record.PayloadAs<KeyRenamed>(), connection, transaction),
            nameof(ReferenceTextProvided) => ProvideReferenceTextAsync(record.PayloadAs<ReferenceTextProvided>(), connection, transaction),
            nameof(ReferenceTextWithdrawn) => WithdrawReferenceTextAsync(record.PayloadAs<ReferenceTextWithdrawn>(), connection, transaction),
            nameof(TranslationProvided) => ProvideTextAsync(record.PayloadAs<TranslationProvided>(), connection, transaction),
            nameof(TranslationWithdrawn) => WithdrawTextAsync(record.PayloadAs<TranslationWithdrawn>(), connection, transaction),
            _ => throw new InvalidOperationException($"Projection {Schema} does not handle {record.Type}."),
        };

    private static Task AddProjectAsync(ProjectAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            INSERT INTO translation_delivery.projects (project_id, team_id, reference_language_code)
            VALUES (@Id, @TeamId, @ReferenceLanguage);

            INSERT INTO translation_delivery.languages (project_id, language_code, parent_language_code)
            VALUES (@Id, @ReferenceLanguage, NULL);
            """, e, transaction);

    private static Task AddLanguageAsync(LanguageAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            INSERT INTO translation_delivery.languages (project_id, language_code, parent_language_code)
            VALUES (@ProjectId, @LanguageCode, @ParentLanguageCode);

            INSERT INTO translation_delivery.texts (project_id, language_code, key_id, key_name, text, source_language_code)
            SELECT project_id, @LanguageCode, key_id, key_name, text, source_language_code
            FROM translation_delivery.texts
            WHERE project_id = @ProjectId AND language_code = @ParentLanguageCode;
            """, e, transaction);

    private static Task AddKeyAsync(KeyAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            INSERT INTO translation_delivery.texts (project_id, language_code, key_id, key_name)
            SELECT project_id, language_code, @Id, @Name
            FROM translation_delivery.languages
            WHERE project_id = @ProjectId;
            """, e, transaction);

    private static Task RenameKeyAsync(KeyRenamed e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            UPDATE translation_delivery.texts
            SET key_name = @Name
            WHERE project_id = @ProjectId AND key_id = @KeyId;
            """, e, transaction);

    private static async Task ProvideReferenceTextAsync(ReferenceTextProvided e, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var referenceLanguageCode = await ReferenceLanguageCodeAsync(e.ProjectId, connection, transaction);

        await ProvideTextAsync(new TranslationProvided(e.KeyId, e.ProjectId, referenceLanguageCode, e.Text), connection, transaction);
    }

    private static Task ProvideTextAsync(TranslationProvided e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
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
            """, e, transaction);

    private static async Task WithdrawReferenceTextAsync(ReferenceTextWithdrawn e, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var referenceLanguageCode = await ReferenceLanguageCodeAsync(e.ProjectId, connection, transaction);

        await WithdrawTextAsync(new TranslationWithdrawn(e.KeyId, e.ProjectId, referenceLanguageCode), connection, transaction);
    }

    private static Task WithdrawTextAsync(TranslationWithdrawn e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
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
            """, e, transaction);

    private static async Task<string> ReferenceLanguageCodeAsync(Guid projectId, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        (await connection.ExecuteScalarAsync<string>(
            "SELECT reference_language_code FROM translation_delivery.projects WHERE project_id = @projectId",
            new { projectId }, transaction))!;
}
