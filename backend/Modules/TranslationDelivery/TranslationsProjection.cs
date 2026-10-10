using System.Text.Json;
using CCC.Domain.Events;
using CCC.EventStore;
using Dapper;
using Npgsql;

namespace CCC.Modules.TranslationDelivery;

// One row per key and project language, holding the text that language shows:
// its own text, or else the text it inherits from the nearest ancestor that has
// one. The reference language is the root of every language tree.
sealed class TranslationsProjection : IProjection
{
    public string Schema => "translation_delivery";

    public int Version => 1;

    public IReadOnlyCollection<string> EventTypes { get; } =
    [
        nameof(ProjectAdded),
        nameof(LanguageAdded),
        nameof(KeyAdded),
        nameof(KeyRenamed),
        nameof(ReferenceTextProvided),
        nameof(TranslationProvided),
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
            nameof(ProjectAdded) => connection.ExecuteAsync("""
                INSERT INTO translation_delivery.projects (project_id, team_id, reference_language_code)
                VALUES (@Id, @TeamId, @ReferenceLanguage);

                INSERT INTO translation_delivery.languages (project_id, language_code, parent_language_code)
                VALUES (@Id, @ReferenceLanguage, NULL);
                """, Payload<ProjectAdded>(record), transaction),

            nameof(LanguageAdded) => connection.ExecuteAsync("""
                INSERT INTO translation_delivery.languages (project_id, language_code, parent_language_code)
                VALUES (@ProjectId, @LanguageCode, @ParentLanguageCode);

                INSERT INTO translation_delivery.texts (project_id, language_code, key_id, key_name, text, source_language_code)
                SELECT project_id, @LanguageCode, key_id, key_name, text, source_language_code
                FROM translation_delivery.texts
                WHERE project_id = @ProjectId AND language_code = @ParentLanguageCode;
                """, Payload<LanguageAdded>(record), transaction),

            nameof(KeyAdded) => connection.ExecuteAsync("""
                INSERT INTO translation_delivery.texts (project_id, language_code, key_id, key_name)
                SELECT project_id, language_code, @Id, @Name
                FROM translation_delivery.languages
                WHERE project_id = @ProjectId;
                """, Payload<KeyAdded>(record), transaction),

            nameof(KeyRenamed) => connection.ExecuteAsync("""
                UPDATE translation_delivery.texts
                SET key_name = @Name
                WHERE project_id = @ProjectId AND key_id = @KeyId;
                """, Payload<KeyRenamed>(record), transaction),

            nameof(ReferenceTextProvided) => ProvideReferenceTextAsync(Payload<ReferenceTextProvided>(record), connection, transaction),

            nameof(TranslationProvided) => ProvideTextAsync(Payload<TranslationProvided>(record), connection, transaction),

            _ => throw new InvalidOperationException($"Projection {Schema} does not handle {record.Type}."),
        };

    private static async Task ProvideReferenceTextAsync(ReferenceTextProvided e, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var referenceLanguageCode = await connection.ExecuteScalarAsync<string>(
            "SELECT reference_language_code FROM translation_delivery.projects WHERE project_id = @ProjectId",
            e, transaction);

        await ProvideTextAsync(new TranslationProvided(e.KeyId, e.ProjectId, referenceLanguageCode!, e.Text), connection, transaction);
    }

    // Sets the language's own text, then hands it down to every descendant that
    // reaches this language through inherited texts only. A descendant with its own
    // text keeps it, and so does its subtree.
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

    private static T Payload<T>(EventRecord record) => JsonSerializer.Deserialize<T>(record.Payload)!;
}
