namespace Rin.Engine.Extensions;

public static class ObjectExtensions
{
    // public static TOut Mutate<TIn, TOut>(this TIn target, Func<TIn, TOut> mutation)
    // {
    //     return mutation.Invoke(target);
    // }

    public static TIn Mutate<TIn>(this TIn target, Action<TIn> mutation) where TIn : class
    {
        mutation.Invoke(target);
        return target;
    }
}