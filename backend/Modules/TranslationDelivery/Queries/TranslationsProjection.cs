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
        connection.ExecuteAsync(InstallTables().With(transaction, cancellationToken));

    public Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var command = record.Type switch
        {
            nameof(ProjectAdded) => AddProject(record.PayloadAs<ProjectAdded>()),
            nameof(LanguageAdded) => AddLanguage(record.PayloadAs<LanguageAdded>()),
            nameof(KeyAdded) => AddKey(record.PayloadAs<KeyAdded>()),
            nameof(KeyRenamed) => RenameKey(record.PayloadAs<KeyRenamed>()),
            nameof(ReferenceTextProvided) => ProvideReferenceText(record.PayloadAs<ReferenceTextProvided>()),
            nameof(ReferenceTextWithdrawn) => WithdrawReferenceText(record.PayloadAs<ReferenceTextWithdrawn>()),
            nameof(TranslationProvided) => ProvideTranslation(record.PayloadAs<TranslationProvided>()),
            nameof(TranslationWithdrawn) => WithdrawTranslation(record.PayloadAs<TranslationWithdrawn>()),
            _ => throw new InvalidOperationException($"Projection {Schema} does not handle {record.Type}."),
        };
        return connection.ExecuteAsync(command.With(transaction, cancellationToken));
    }

    private static CommandDefinition InstallTables() =>
        new("""
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
            """);

    private static CommandDefinition AddProject(ProjectAdded e) =>
        new("""
            INSERT INTO translation_delivery.projects (project_id, team_id, reference_language_code)
            VALUES (@Id, @TeamId, @ReferenceLanguage);

            INSERT INTO translation_delivery.languages (project_id, language_code, parent_language_code)
            VALUES (@Id, @ReferenceLanguage, NULL);
            """, e);

    private static CommandDefinition AddLanguage(LanguageAdded e) =>
        new("""
            INSERT INTO translation_delivery.languages (project_id, language_code, parent_language_code)
            VALUES (@ProjectId, @LanguageCode, @ParentLanguageCode);

            INSERT INTO translation_delivery.texts (project_id, language_code, key_id, key_name, text, source_language_code)
            SELECT project_id, @LanguageCode, key_id, key_name, text, source_language_code
            FROM translation_delivery.texts
            WHERE project_id = @ProjectId AND language_code = @ParentLanguageCode;
            """, e);

    private static CommandDefinition AddKey(KeyAdded e) =>
        new("""
            INSERT INTO translation_delivery.texts (project_id, language_code, key_id, key_name)
            SELECT project_id, language_code, @Id, @Name
            FROM translation_delivery.languages
            WHERE project_id = @ProjectId;
            """, e);

    private static CommandDefinition RenameKey(KeyRenamed e) =>
        new("""
            UPDATE translation_delivery.texts
            SET key_name = @Name
            WHERE project_id = @ProjectId AND key_id = @KeyId;
            """, e);

    private static CommandDefinition ProvideReferenceText(ReferenceTextProvided e) =>
        ProvideText(e.KeyId, e.ProjectId, languageCode: null, e.Text);

    private static CommandDefinition ProvideTranslation(TranslationProvided e) =>
        ProvideText(e.KeyId, e.ProjectId, e.LanguageCode, e.Text);

    private static CommandDefinition ProvideText(Guid keyId, Guid projectId, string? languageCode, string text) =>
        new("""
            WITH RECURSIVE
                provided AS (
                    SELECT COALESCE(@LanguageCode, reference_language_code) AS language_code
                    FROM translation_delivery.projects
                    WHERE project_id = @ProjectId
                ),
                inheriting AS (
                    SELECT l.language_code, l.parent_language_code
                    FROM translation_delivery.languages l
                    JOIN translation_delivery.texts t
                        ON t.project_id = l.project_id AND t.language_code = l.language_code AND t.key_id = @KeyId
                    WHERE l.project_id = @ProjectId
                      AND t.source_language_code IS DISTINCT FROM t.language_code
                ),
                receiving AS (
                    SELECT language_code FROM provided
                    UNION ALL
                    SELECT i.language_code FROM inheriting i JOIN receiving r ON i.parent_language_code = r.language_code
                )
            UPDATE translation_delivery.texts
            SET text = @Text, source_language_code = (SELECT language_code FROM provided)
            WHERE project_id = @ProjectId AND key_id = @KeyId
              AND language_code IN (SELECT language_code FROM receiving);
            """, new { KeyId = keyId, ProjectId = projectId, LanguageCode = languageCode, Text = text });

    private static CommandDefinition WithdrawReferenceText(ReferenceTextWithdrawn e) =>
        WithdrawText(e.KeyId, e.ProjectId, languageCode: null);

    private static CommandDefinition WithdrawTranslation(TranslationWithdrawn e) =>
        WithdrawText(e.KeyId, e.ProjectId, e.LanguageCode);

    private static CommandDefinition WithdrawText(Guid keyId, Guid projectId, string? languageCode) =>
        new("""
            WITH
                withdrawn AS (
                    SELECT COALESCE(@LanguageCode, reference_language_code) AS language_code
                    FROM translation_delivery.projects
                    WHERE project_id = @ProjectId
                ),
                parent AS (
                    SELECT p.text, p.source_language_code
                    FROM translation_delivery.languages l
                    JOIN translation_delivery.texts p
                        ON p.project_id = l.project_id AND p.language_code = l.parent_language_code AND p.key_id = @KeyId
                    WHERE l.project_id = @ProjectId AND l.language_code = (SELECT language_code FROM withdrawn)
                )
            UPDATE translation_delivery.texts
            SET text = (SELECT text FROM parent), source_language_code = (SELECT source_language_code FROM parent)
            WHERE project_id = @ProjectId AND key_id = @KeyId
              AND source_language_code = (SELECT language_code FROM withdrawn);
            """, new { KeyId = keyId, ProjectId = projectId, LanguageCode = languageCode });
}
