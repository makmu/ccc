namespace CCC.Base.Dcb;

sealed class AllStateModel(IStateModel<bool>[] models) : IStateModel<bool>
{
    private bool _loaded;

    public bool Value => _loaded ? models.All(model => model.Value) : throw StateModel.NotLoaded(Filter);

    public EventFilter Filter => new(models.SelectMany(model => model.Filter.Entries).ToList());

    public void Load(IEnumerable<EventRecord> records)
    {
        foreach (var model in models)
            model.Load(records.Where(model.Filter.Matches));
        _loaded = true;
    }
}
