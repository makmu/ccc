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

    public Task InstallAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
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
            nameof(TranslationProvided) => ProvideTranslationAsync(record.PayloadAs<TranslationProvided>(), connection, transaction, cancellationToken),
            nameof(TranslationWithdrawn) => WithdrawTranslationAsync(record.PayloadAs<TranslationWithdrawn>(), connection, transaction, cancellationToken),
            _ => throw new InvalidOperationException($"Projection {Schema} does not handle {record.Type}."),
        };

    private static Task AddProjectAsync(ProjectAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO translation_editor.projects (project_id, team_id, reference_language)
            VALUES (@Id, @TeamId, @ReferenceLanguage);
            """, e, transaction, cancellationToken: cancellationToken));

    private static Task AddLanguageAsync(LanguageAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO translation_editor.languages (project_id, language_code)
            VALUES (@ProjectId, @LanguageCode);
            """, e, transaction, cancellationToken: cancellationToken));

    private static Task AddKeyAsync(KeyAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO translation_editor.keys (key_id, project_id, name)
            VALUES (@Id, @ProjectId, @Name);
            """, e, transaction, cancellationToken: cancellationToken));

    private static Task RenameKeyAsync(KeyRenamed e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            UPDATE translation_editor.keys SET name = @Name WHERE key_id = @KeyId;
            """, e, transaction, cancellationToken: cancellationToken));

    private static Task ProvideReferenceTextAsync(ReferenceTextProvided e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            UPDATE translation_editor.keys SET reference_text = @Text WHERE key_id = @KeyId;
            """, e, transaction, cancellationToken: cancellationToken));

    private static Task WithdrawReferenceTextAsync(ReferenceTextWithdrawn e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            UPDATE translation_editor.keys SET reference_text = NULL WHERE key_id = @KeyId;
            """, e, transaction, cancellationToken: cancellationToken));

    private static Task ProvideTranslationAsync(TranslationProvided e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO translation_editor.translations (key_id, language_code, text)
            VALUES (@KeyId, @LanguageCode, @Text)
            ON CONFLICT (key_id, language_code) DO UPDATE SET text = EXCLUDED.text;
            """, e, transaction, cancellationToken: cancellationToken));

    private static Task WithdrawTranslationAsync(TranslationWithdrawn e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            DELETE FROM translation_editor.translations
            WHERE key_id = @KeyId AND language_code = @LanguageCode;
            """, e, transaction, cancellationToken: cancellationToken));
}
