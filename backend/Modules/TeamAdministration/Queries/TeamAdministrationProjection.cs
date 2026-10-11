using CCC.Domain.Events;
using CCC.EventStore;
using Dapper;
using Npgsql;

namespace CCC.Modules.TeamAdministration.Queries;

sealed class TeamAdministrationProjection : IProjection
{
    public string Schema => "team_administration";

    public int Version => 1;

    public EventHandlers<CommandDefinition> Handlers { get; } = new EventHandlers<CommandDefinition>()
        .On<ProjectAdded>(AddProject)
        .On<LanguageAdded>(AddLanguage);

    public Task InstallAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(InstallTables().With(transaction, cancellationToken));

    public Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(Handlers.Handle(record).With(transaction, cancellationToken));

    private static CommandDefinition InstallTables() =>
        new("""
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
            """);

    private static CommandDefinition AddProject(ProjectAdded e) =>
        new("""
            INSERT INTO team_administration.projects (project_id, team_id, name, reference_language)
            VALUES (@Id, @TeamId, @Name, @ReferenceLanguage);

            INSERT INTO team_administration.languages (project_id, language_code, parent_language_code)
            VALUES (@Id, @ReferenceLanguage, NULL);
            """, e);

    private static CommandDefinition AddLanguage(LanguageAdded e) =>
        new("""
            INSERT INTO team_administration.languages (project_id, language_code, parent_language_code)
            VALUES (@ProjectId, @LanguageCode, @ParentLanguageCode);
            """, e);
}
