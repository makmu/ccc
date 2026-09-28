namespace CCC.EventStore;

// Type-erased view of a StateModel<T> so the builder can hold many
// differently-typed models in one array. The strongly-typed State stays on the
// concrete StateModel<T> the caller holds; the builder only needs to know what
// to query (Filter) and how to fold an event in (Apply).
public interface IStateModel
{
    EventFilter Filter { get; }
    void Apply(EventRecord record);
}
