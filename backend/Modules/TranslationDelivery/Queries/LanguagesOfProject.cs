using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TranslationDelivery.Queries;

public record LanguagesOfProject(TeamId TeamId, ProjectId ProjectId);

sealed class LanguagesOfProjectHandler(IStateLoader loader) : IQueryHandler<LanguagesOfProject, IReadOnlyList<string>?>
{
    public async Task<IReadOnlyList<string>?> HandleAsync(LanguagesOfProject query, CancellationToken cancellationToken)
    {
        var inTeam = Project.InTeam(query.TeamId, query.ProjectId);
        var languages = Project.Languages(query.ProjectId);
        await loader.LoadAsync([inTeam, languages], cancellationToken);

        return inTeam.IsTrue() ? languages.List().Select(language => language.Value).ToList() : null;
    }
}
