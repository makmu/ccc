using CCC.Base.Dcb;
namespace CCC.EventStore;

public interface IStateLoader
{
    Task<StateContext> LoadAsync(IReadOnlyList<IStateModel> models, CancellationToken cancellationToken);
}
