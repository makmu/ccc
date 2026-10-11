namespace CCC.Base.Dcb;

public static class StateModelExtensions
{
    public static bool IsTrue(this IStateModel<bool> model) => model.Value;

    public static bool IsFalse(this IStateModel<bool> model) => !model.Value;

    public static bool IsNull<T>(this IStateModel<T?> model, out T value) where T : struct
    {
        var nullable = model.Value;
        value = nullable.GetValueOrDefault();
        return nullable is null;
    }
    
    public static IReadOnlyList<T> List<T>(this IStateModel<IReadOnlyList<T>> model) => model.Value;
}
