using CCC.Domain.Events;
using CCC.EventStore;
using Dapper;
using Npgsql;

namespace CCC.Modules.TeamAdministration.Queries;

sealed class TeamAdministrationProjection : IProjection
{
    public string Schema => "team_administration";

    public int Version => 1;

    public IReadOnlyCollection<string> EventTypes { get; } =
    [
        nameof(ProjectAdded),
        nameof(LanguageAdded),
    ];

    public Task InstallAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
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
            """, transaction: transaction, cancellationToken: cancellationToken));

    public Task ApplyAsync(EventRecord record, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        record.Type switch
        {
            nameof(ProjectAdded) => AddProjectAsync(record.PayloadAs<ProjectAdded>(), connection, transaction, cancellationToken),
            nameof(LanguageAdded) => AddLanguageAsync(record.PayloadAs<LanguageAdded>(), connection, transaction, cancellationToken),
            _ => throw new InvalidOperationException($"Projection {Schema} does not handle {record.Type}."),
        };

    private static Task AddProjectAsync(ProjectAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO team_administration.projects (project_id, team_id, name, reference_language)
            VALUES (@Id, @TeamId, @Name, @ReferenceLanguage);

            INSERT INTO team_administration.languages (project_id, language_code, parent_language_code)
            VALUES (@Id, @ReferenceLanguage, NULL);
            """, e, transaction, cancellationToken: cancellationToken));

    private static Task AddLanguageAsync(LanguageAdded e, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO team_administration.languages (project_id, language_code, parent_language_code)
            VALUES (@ProjectId, @LanguageCode, @ParentLanguageCode);
            """, e, transaction, cancellationToken: cancellationToken));
}
