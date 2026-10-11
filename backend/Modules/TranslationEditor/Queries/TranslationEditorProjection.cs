using CCC.Domain.Events;
using CCC.EventStore;
using CCC.ProjectionBase;
using Dapper;
using Npgsql;

namespace CCC.Modules.TranslationEditor.Queries;

sealed class TranslationEditorProjection : IProjection
{
    public string Schema => "translation_editor";

    public int Version => 2;

    public EventHandlers<CommandDefinition> Handlers { get; } = new EventHandlers<CommandDefinition>()
        .On<ProjectAdded>(AddProject)
        .On<LanguageAdded>(AddLanguage)
        .On<KeyAdded>(AddKey)
        .On<KeyRenamed>(RenameKey)
        .On<ReferenceTextProvided>(ProvideReferenceText)
        .On<ReferenceTextWithdrawn>(WithdrawReferenceText)
        .On<TranslationProvided>(ProvideTranslation)
        .On<TranslationWithdrawn>(WithdrawTranslation);

    public Task InstallAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(InstallTables().With(transaction, cancellationToken));

    public Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(Handlers.Handle(record).With(transaction, cancellationToken));

    private static CommandDefinition InstallTables() =>
        new("""
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
            """);

    private static CommandDefinition AddProject(ProjectAdded e) =>
        new("""
            INSERT INTO translation_editor.projects (project_id, team_id, reference_language)
            VALUES (@Id, @TeamId, @ReferenceLanguage);
            """, e);

    private static CommandDefinition AddLanguage(LanguageAdded e) =>
        new("""
            INSERT INTO translation_editor.languages (project_id, language_code)
            VALUES (@ProjectId, @LanguageCode);
            """, e);

    private static CommandDefinition AddKey(KeyAdded e) =>
        new("""
            INSERT INTO translation_editor.keys (key_id, project_id, name)
            VALUES (@Id, @ProjectId, @Name);
            """, e);

    private static CommandDefinition RenameKey(KeyRenamed e) =>
        new("""
            UPDATE translation_editor.keys SET name = @Name WHERE key_id = @KeyId;
            """, e);

    private static CommandDefinition ProvideReferenceText(ReferenceTextProvided e) =>
        new("""
            UPDATE translation_editor.keys SET reference_text = @Text WHERE key_id = @KeyId;
            """, e);

    private static CommandDefinition WithdrawReferenceText(ReferenceTextWithdrawn e) =>
        new("""
            UPDATE translation_editor.keys SET reference_text = NULL WHERE key_id = @KeyId;
            """, e);

    private static CommandDefinition ProvideTranslation(TranslationProvided e) =>
        new("""
            INSERT INTO translation_editor.translations (key_id, language_code, text)
            VALUES (@KeyId, @LanguageCode, @Text)
            ON CONFLICT (key_id, language_code) DO UPDATE SET text = EXCLUDED.text;
            """, e);

    private static CommandDefinition WithdrawTranslation(TranslationWithdrawn e) =>
        new("""
            DELETE FROM translation_editor.translations
            WHERE key_id = @KeyId AND language_code = @LanguageCode;
            """, e);
}
