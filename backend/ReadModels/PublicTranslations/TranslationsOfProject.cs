using CCC.Domain.Types;
using CCC.EventStore;
using Dapper;

namespace CCC.ReadModels.PublicTranslations;

public record TranslationsOfProject(TeamId TeamId, ProjectId ProjectId, LanguageCode LanguageCode);

public record Translation(Guid KeyId, string KeyName, string? Text, string? SourceLanguageCode);

public sealed class TranslationsOfProjectHandler(ReadModelDatabase database)
{
    public async Task<IReadOnlyList<Translation>?> HandleAsync(TranslationsOfProject query)
    {
        var parameters = new
        {
            TeamId = query.TeamId.Value,
            ProjectId = query.ProjectId.Value,
            LanguageCode = query.LanguageCode.Value,
        };

        await using var connection = await database.OpenAsync();

        var languageInProject = await connection.ExecuteScalarAsync<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM public_translations.projects p
                JOIN public_translations.languages l ON l.project_id = p.project_id
                WHERE p.project_id = @ProjectId AND p.team_id = @TeamId AND l.language_code = @LanguageCode
            )
            """, parameters);

        if (!languageInProject)
            return null;

        var translations = await connection.QueryAsync<Translation>("""
            SELECT key_id AS KeyId, key_name AS KeyName, text AS Text, source_language_code AS SourceLanguageCode
            FROM public_translations.texts
            WHERE project_id = @ProjectId AND language_code = @LanguageCode
            ORDER BY key_name
            """, parameters);

        return translations.AsList();
    }
}
