using CCC.Abstractions;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TranslationDelivery;

public record LanguagesOfProject(TeamId TeamId, ProjectId ProjectId);

sealed class LanguagesOfProjectHandler(IStateLoader loader) : IQueryHandler<LanguagesOfProject, IReadOnlyList<string>?>
{
    public async Task<IReadOnlyList<string>?> HandleAsync(LanguagesOfProject query)
    {
        var (inTeam, languages, _) = await loader.LoadAsync(
            Project.InTeam(query.TeamId, query.ProjectId),
            Project.Languages(query.ProjectId));

        return inTeam ? languages.Select(language => language.Value).ToList() : null;
    }
}
