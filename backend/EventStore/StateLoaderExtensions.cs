namespace CCC.EventStore;

public static class StateLoaderExtensions
{
    public static async Task<(T1, StateContext)> LoadAsync<T1>(
        this IStateLoader loader, IStateModel<T1> model1,
        CancellationToken cancellationToken)
    {
        var context = await loader.LoadAsync([model1], cancellationToken);
        return (model1.State, context);
    }

    public static async Task<(T1, T2, StateContext)> LoadAsync<T1, T2>(
        this IStateLoader loader, IStateModel<T1> model1, IStateModel<T2> model2,
        CancellationToken cancellationToken)
    {
        var context = await loader.LoadAsync([model1, model2], cancellationToken);
        return (model1.State, model2.State, context);
    }

    public static async Task<(T1, T2, T3, StateContext)> LoadAsync<T1, T2, T3>(
        this IStateLoader loader, IStateModel<T1> model1, IStateModel<T2> model2, IStateModel<T3> model3,
        CancellationToken cancellationToken)
    {
        var context = await loader.LoadAsync([model1, model2, model3], cancellationToken);
        return (model1.State, model2.State, model3.State, context);
    }

    public static async Task<(T1, T2, T3, T4, StateContext)> LoadAsync<T1, T2, T3, T4>(
        this IStateLoader loader, IStateModel<T1> model1, IStateModel<T2> model2, IStateModel<T3> model3, IStateModel<T4> model4,
        CancellationToken cancellationToken)
    {
        var context = await loader.LoadAsync([model1, model2, model3, model4], cancellationToken);
        return (model1.State, model2.State, model3.State, model4.State, context);
    }

    public static async Task<(T1, T2, T3, T4, T5, StateContext)> LoadAsync<T1, T2, T3, T4, T5>(
        this IStateLoader loader, IStateModel<T1> model1, IStateModel<T2> model2, IStateModel<T3> model3, IStateModel<T4> model4, IStateModel<T5> model5,
        CancellationToken cancellationToken)
    {
        var context = await loader.LoadAsync([model1, model2, model3, model4, model5], cancellationToken);
        return (model1.State, model2.State, model3.State, model4.State, model5.State, context);
    }

    public static async Task<(T1, T2, T3, T4, T5, T6, StateContext)> LoadAsync<T1, T2, T3, T4, T5, T6>(
        this IStateLoader loader, IStateModel<T1> model1, IStateModel<T2> model2, IStateModel<T3> model3, IStateModel<T4> model4, IStateModel<T5> model5, IStateModel<T6> model6,
        CancellationToken cancellationToken)
    {
        var context = await loader.LoadAsync([model1, model2, model3, model4, model5, model6], cancellationToken);
        return (model1.State, model2.State, model3.State, model4.State, model5.State, model6.State, context);
    }
}
