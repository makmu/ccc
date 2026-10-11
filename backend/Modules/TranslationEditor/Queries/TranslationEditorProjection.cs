using System.Text.Json;
using CCC.Domain.Events;
using CCC.EventStore;
using Dapper;
using Npgsql;

namespace CCC.Modules.TranslationEditor.Queries;

sealed class TranslationEditorProjection : IProjection
{
    public string Schema => "translation_editor";

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
            CREATE TABLE IF NOT EXISTS translation_editor.projects (
                project_id         UUID PRIMARY KEY,
                team_id            UUID NOT NULL,
                reference_language TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS translation_editor.languages (
                project_id    UUID NOT NULL,
                language_code TEXT NOT NULL,
                PRIMARY KEY (project_id, language_code)
            );

            CREATE TABLE IF NOT EXISTS translation_editor.keys (
                key_id         UUID PRIMARY KEY,
                project_id     UUID NOT NULL,
                name           TEXT NOT NULL,
                reference_text TEXT
            );

            CREATE TABLE IF NOT EXISTS translation_editor.translations (
                key_id        UUID NOT NULL,
                language_code TEXT NOT NULL,
                text          TEXT NOT NULL,
                PRIMARY KEY (key_id, language_code)
            );
            """, transaction: transaction);

    public Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        record.Type switch
        {
            nameof(ProjectAdded) => AddProjectAsync(Payload<ProjectAdded>(record), connection, transaction),
            nameof(LanguageAdded) => AddLanguageAsync(Payload<LanguageAdded>(record), connection, transaction),
            nameof(KeyAdded) => AddKeyAsync(Payload<KeyAdded>(record), connection, transaction),
            nameof(KeyRenamed) => RenameKeyAsync(Payload<KeyRenamed>(record), connection, transaction),
            nameof(ReferenceTextProvided) => ProvideReferenceTextAsync(Payload<ReferenceTextProvided>(record), connection, transaction),
            nameof(ReferenceTextWithdrawn) => WithdrawReferenceTextAsync(Payload<ReferenceTextWithdrawn>(record), connection, transaction),
            nameof(TranslationProvided) => ProvideTranslationAsync(Payload<TranslationProvided>(record), connection, transaction),
            nameof(TranslationWithdrawn) => WithdrawTranslationAsync(Payload<TranslationWithdrawn>(record), connection, transaction),
            _ => throw new InvalidOperationException($"Projection {Schema} does not handle {record.Type}."),
        };

    private static Task AddProjectAsync(ProjectAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            INSERT INTO translation_editor.projects (project_id, team_id, reference_language)
            VALUES (@Id, @TeamId, @ReferenceLanguage);
            """, e, transaction);

    private static Task AddLanguageAsync(LanguageAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            INSERT INTO translation_editor.languages (project_id, language_code)
            VALUES (@ProjectId, @LanguageCode);
            """, e, transaction);

    private static Task AddKeyAsync(KeyAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            INSERT INTO translation_editor.keys (key_id, project_id, name)
            VALUES (@Id, @ProjectId, @Name);
            """, e, transaction);

    private static Task RenameKeyAsync(KeyRenamed e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            UPDATE translation_editor.keys SET name = @Name WHERE key_id = @KeyId;
            """, e, transaction);

    private static Task ProvideReferenceTextAsync(ReferenceTextProvided e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            UPDATE translation_editor.keys SET reference_text = @Text WHERE key_id = @KeyId;
            """, e, transaction);

    private static Task WithdrawReferenceTextAsync(ReferenceTextWithdrawn e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            UPDATE translation_editor.keys SET reference_text = NULL WHERE key_id = @KeyId;
            """, e, transaction);

    private static Task ProvideTranslationAsync(TranslationProvided e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            INSERT INTO translation_editor.translations (key_id, language_code, text)
            VALUES (@KeyId, @LanguageCode, @Text)
            ON CONFLICT (key_id, language_code) DO UPDATE SET text = EXCLUDED.text;
            """, e, transaction);

    private static Task WithdrawTranslationAsync(TranslationWithdrawn e, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            DELETE FROM translation_editor.translations
            WHERE key_id = @KeyId AND language_code = @LanguageCode;
            """, e, transaction);

    private static T Payload<T>(EventRecord record) => JsonSerializer.Deserialize<T>(record.Payload)!;
}
