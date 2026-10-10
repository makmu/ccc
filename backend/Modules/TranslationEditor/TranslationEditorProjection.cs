using System.Text.Json;
using CCC.Domain.Events;
using CCC.EventStore;
using Dapper;
using Npgsql;

namespace CCC.Modules.TranslationEditor;

sealed class TranslationEditorProjection : IProjection
{
    public string Schema => "translation_editor";

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
            nameof(ProjectAdded) => connection.ExecuteAsync("""
                INSERT INTO translation_editor.projects (project_id, team_id, reference_language)
                VALUES (@Id, @TeamId, @ReferenceLanguage);
                """, Payload<ProjectAdded>(record), transaction),

            nameof(LanguageAdded) => connection.ExecuteAsync("""
                INSERT INTO translation_editor.languages (project_id, language_code)
                VALUES (@ProjectId, @LanguageCode);
                """, Payload<LanguageAdded>(record), transaction),

            nameof(KeyAdded) => connection.ExecuteAsync("""
                INSERT INTO translation_editor.keys (key_id, project_id, name)
                VALUES (@Id, @ProjectId, @Name);
                """, Payload<KeyAdded>(record), transaction),

            nameof(KeyRenamed) => connection.ExecuteAsync("""
                UPDATE translation_editor.keys SET name = @Name WHERE key_id = @KeyId;
                """, Payload<KeyRenamed>(record), transaction),

            nameof(ReferenceTextProvided) => connection.ExecuteAsync("""
                UPDATE translation_editor.keys SET reference_text = @Text WHERE key_id = @KeyId;
                """, Payload<ReferenceTextProvided>(record), transaction),

            nameof(TranslationProvided) => connection.ExecuteAsync("""
                INSERT INTO translation_editor.translations (key_id, language_code, text)
                VALUES (@KeyId, @LanguageCode, @Text)
                ON CONFLICT (key_id, language_code) DO UPDATE SET text = EXCLUDED.text;
                """, Payload<TranslationProvided>(record), transaction),

            _ => throw new InvalidOperationException($"Projection {Schema} does not handle {record.Type}."),
        };

    private static T Payload<T>(EventRecord record) => JsonSerializer.Deserialize<T>(record.Payload)!;
}
