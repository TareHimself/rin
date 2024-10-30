using System.Runtime.InteropServices;

namespace rin.Core.Extensions;

public static class EnumerableExtensions
{
    
    public static IEnumerable<T> AsReversed<T>(this T[] target)
    {
        for (var i = target.Length - 1; i > -1; i--)
        {
            if (target.Length <= i) continue;
            yield return target[i];
        }
    }

    public static IEnumerable<T> AsReversed<T>(this IEnumerable<T> target) => target.ToArray().AsReversed();
    
    public static IEnumerable<T> AsReversed<T>(this List<T> target)
    {
        for (var i = target.Count - 1; i > -1; i--)
        {
            if (target.Count <= i) continue;
            yield return target[i];
        }
    }

    public static ulong ByteSize<T>(this List<T> target)
    {
        return (ulong)(Marshal.SizeOf<T>() * target.Count);
    }

    public static ulong ByteSize<T>(this T[] target)
    {
        return (ulong)(Marshal.SizeOf<T>() * target.Length);
    }


    public static List<T> UpdateIndex<T>(this List<T> target, int index, Func<T, T> updater)
    {
        target[index] = updater(target[index]);
        return target;
    }
    
    public static T? TryIndex<T>(this T[] target, int index) where T : class?
    {
        if (target.Length <= index || index < 0) return null;

        return target[index];
    }
}