using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.ReadModels;
using Dapper;

namespace CCC.Modules.TranslationDelivery.Queries;

public record TranslationsOfProject(TeamId TeamId, ProjectId ProjectId, LanguageCode LanguageCode);

public record Translation(Guid KeyId, string KeyName, string? Text, string? SourceLanguageCode);

sealed class TranslationsOfProjectHandler(ReadModelDatabase database)
    : IQueryHandler<TranslationsOfProject, IReadOnlyList<Translation>?>
{
    public async Task<IReadOnlyList<Translation>?> HandleAsync(TranslationsOfProject query, CancellationToken cancellationToken)
    {
        var parameters = new
        {
            TeamId = query.TeamId.Value,
            ProjectId = query.ProjectId.Value,
            LanguageCode = query.LanguageCode.Value,
        };

        await using var connection = await database.OpenAsync(cancellationToken);

        var languageInProject = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
            SELECT EXISTS (
                SELECT 1
                FROM translation_delivery.projects p
                JOIN translation_delivery.languages l ON l.project_id = p.project_id
                WHERE p.project_id = @ProjectId AND p.team_id = @TeamId AND l.language_code = @LanguageCode
            )
            """, parameters, cancellationToken: cancellationToken));

        if (!languageInProject)
            return null;

        var translations = await connection.QueryAsync<Translation>(new CommandDefinition("""
            SELECT key_id AS KeyId, key_name AS KeyName, text AS Text, source_language_code AS SourceLanguageCode
            FROM translation_delivery.texts
            WHERE project_id = @ProjectId AND language_code = @LanguageCode
            ORDER BY key_name
            """, parameters, cancellationToken: cancellationToken));

        return translations.AsList();
    }
}
