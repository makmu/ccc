using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.EventStore;
using Dapper;

namespace CCC.Modules.TeamAdministration.Queries;

public record ProjectsOfTeam(TeamId TeamId);

public record ProjectSummary(Guid Id, string Name, string ReferenceLanguage, IReadOnlyList<ProjectLanguage> Languages);

public record ProjectLanguage(string Code, string? ParentCode);

sealed class ProjectsOfTeamHandler(ReadModelDatabase database) : IQueryHandler<ProjectsOfTeam, IReadOnlyList<ProjectSummary>>
{
    public async Task<IReadOnlyList<ProjectSummary>> HandleAsync(ProjectsOfTeam query, CancellationToken cancellationToken)
    {
        var parameters = new { TeamId = query.TeamId.Value };
        await using var connection = await database.OpenAsync(cancellationToken);

        var projects = await connection.QueryAsync<(Guid Id, string Name, string ReferenceLanguage)>(new CommandDefinition("""
            SELECT project_id, name, reference_language
            FROM team_administration.projects
            WHERE team_id = @TeamId
            ORDER BY name
            """, parameters, cancellationToken: cancellationToken));

        var languages = (await connection.QueryAsync<(Guid ProjectId, string Code, string? ParentCode)>(new CommandDefinition("""
            SELECT l.project_id, l.language_code, l.parent_language_code
            FROM team_administration.languages l
            JOIN team_administration.projects p ON p.project_id = l.project_id
            WHERE p.team_id = @TeamId
            ORDER BY l.language_code
            """, parameters, cancellationToken: cancellationToken))).ToLookup(language => language.ProjectId);

        return projects
            .Select(project => new ProjectSummary(
                project.Id,
                project.Name,
                project.ReferenceLanguage,
                languages[project.Id].Select(language => new ProjectLanguage(language.Code, language.ParentCode)).ToList()))
            .ToList();
    }
}
