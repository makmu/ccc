namespace CCC.Base.Dcb;

public interface IStateModel
{
    EventFilter Filter { get; }

    // Takes the records matching Filter in position order, and folds them from the
    // initial state, so loading the same model again doesn't apply a record twice.
    void Load(IEnumerable<EventRecord> records);
}

public interface IStateModel<out T> : IStateModel
{
    T Value { get; }
}
