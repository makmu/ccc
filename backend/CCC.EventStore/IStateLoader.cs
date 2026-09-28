namespace CCC.EventStore;

public interface IStateLoader
{
    Task<StateContext> LoadAsync(params IStateModel[] models);
}
