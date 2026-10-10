using System.Text.Json;
using CCC.Domain.Events;
using CCC.EventStore;
using Dapper;
using Npgsql;

namespace CCC.Modules.TeamAdministration;

sealed class TeamAdministrationProjection : IProjection
{
    public string Schema => "team_administration";

    public int Version => 1;

    public IReadOnlyCollection<string> EventTypes { get; } =
    [
        nameof(ProjectAdded),
        nameof(LanguageAdded),
    ];

    public Task InstallAsync(NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS team_administration.projects (
                project_id         UUID PRIMARY KEY,
                team_id            UUID NOT NULL,
                name               TEXT NOT NULL,
                reference_language TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS team_administration.languages (
                project_id           UUID NOT NULL,
                language_code        TEXT NOT NULL,
                parent_language_code TEXT,
                PRIMARY KEY (project_id, language_code)
            );
            """, transaction: transaction);

    public Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        record.Type switch
        {
            nameof(ProjectAdded) => connection.ExecuteAsync("""
                INSERT INTO team_administration.projects (project_id, team_id, name, reference_language)
                VALUES (@Id, @TeamId, @Name, @ReferenceLanguage);

                INSERT INTO team_administration.languages (project_id, language_code, parent_language_code)
                VALUES (@Id, @ReferenceLanguage, NULL);
                """, Payload<ProjectAdded>(record), transaction),

            nameof(LanguageAdded) => connection.ExecuteAsync("""
                INSERT INTO team_administration.languages (project_id, language_code, parent_language_code)
                VALUES (@ProjectId, @LanguageCode, @ParentLanguageCode);
                """, Payload<LanguageAdded>(record), transaction),

            _ => throw new InvalidOperationException($"Projection {Schema} does not handle {record.Type}."),
        };

    private static T Payload<T>(EventRecord record) => JsonSerializer.Deserialize<T>(record.Payload)!;
}
