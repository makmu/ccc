namespace CCC.EventStore;

public interface IStateModel
{
    EventFilter Filter { get; }
    void Apply(EventRecord record);
}
