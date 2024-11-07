namespace rin.Core.Extensions;

public static class ObjectExtensions
{
    public static T Apply<T>(this T target, Action<T> mutation)
    {
        mutation(target);
        return target;
    }
}