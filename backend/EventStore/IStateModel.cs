namespace CCC.EventStore;

public interface IStateModel
{
    EventFilter Filter { get; }
    void Apply(EventRecord record);
}

public interface IStateModel<out T> : IStateModel
{
    T State { get; }
}
