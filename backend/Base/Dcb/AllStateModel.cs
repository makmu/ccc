namespace CCC.Base.Dcb;

sealed class AllStateModel(IStateModel<bool>[] models) : IStateModel<bool>
{
    private readonly EventFilter[] _filters = models.Select(model => model.Filter).ToArray();

    public bool State => models.All(model => model.State);

    public EventFilter Filter => new(_filters.SelectMany(filter => filter.Entries).ToList());

    public void Apply(EventRecord record)
    {
        for (var i = 0; i < models.Length; i++)
        {
            if (_filters[i].Matches(record))
                models[i].Apply(record);
        }
    }
}
