using CCC.Abstractions;
using CCC.Domain.Types;
using CCC.EventStore;
using Dapper;

namespace CCC.Modules.TranslationEditor.Queries;

public record KeysOfProject(TeamId TeamId, ProjectId ProjectId);

public record ProjectKeys(string ReferenceLanguage, IReadOnlyList<string> Languages, IReadOnlyList<KeyTexts> Keys);

public record KeyTexts(Guid Id, string Name, string? ReferenceText, IReadOnlyDictionary<string, string> Translations);

sealed class KeysOfProjectHandler(ReadModelDatabase database) : IQueryHandler<KeysOfProject, ProjectKeys?>
{
    public async Task<ProjectKeys?> HandleAsync(KeysOfProject query)
    {
        var parameters = new { TeamId = query.TeamId.Value, ProjectId = query.ProjectId.Value };
        await using var connection = await database.OpenAsync();

        var referenceLanguage = await connection.QuerySingleOrDefaultAsync<string>("""
            SELECT reference_language
            FROM translation_editor.projects
            WHERE project_id = @ProjectId AND team_id = @TeamId
            """, parameters);

        if (referenceLanguage is null)
            return null;

        var languages = await connection.QueryAsync<string>("""
            SELECT language_code
            FROM translation_editor.languages
            WHERE project_id = @ProjectId
            ORDER BY language_code
            """, parameters);

        var keys = await connection.QueryAsync<(Guid Id, string Name, string? ReferenceText)>("""
            SELECT key_id, name, reference_text
            FROM translation_editor.keys
            WHERE project_id = @ProjectId
            ORDER BY name
            """, parameters);

        var translations = (await connection.QueryAsync<(Guid KeyId, string LanguageCode, string Text)>("""
            SELECT t.key_id, t.language_code, t.text
            FROM translation_editor.translations t
            JOIN translation_editor.keys k ON k.key_id = t.key_id
            WHERE k.project_id = @ProjectId
            """, parameters)).ToLookup(translation => translation.KeyId);

        return new ProjectKeys(
            referenceLanguage,
            languages.AsList(),
            keys.Select(key => new KeyTexts(
                    key.Id,
                    key.Name,
                    key.ReferenceText,
                    translations[key.Id].ToDictionary(translation => translation.LanguageCode, translation => translation.Text)))
                .ToList());
    }
}
