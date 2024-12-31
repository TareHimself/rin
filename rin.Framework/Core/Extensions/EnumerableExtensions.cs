using System.Runtime.InteropServices;

namespace rin.Framework.Core.Extensions;

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

    public static IEnumerable<T> AsReversed<T>(this IEnumerable<T> target)
    {
        {
            if (target is List<T> asList)
            {
                for (var i = asList.Count - 1; i > -1; i--)
                {
                    if (asList.Count <= i) continue;
                    yield return asList[i];
                }
            }
        }
        
        {
            if (target is LinkedList<T> asLinkedList)
            {
                var el = asLinkedList.Last;
                while (el != null) {
                    yield return el.Value;
                    el = el.Previous;
                }
            }
        }
        {
            var asArray = target.ToArray();
            for (var i = asArray.Length - 1; i > -1; i--)
            {
                if (asArray.Length <= i) continue;
                yield return asArray[i];
            }
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


    public static bool Empty<T>(this T[] target) => target.Length == 0;
    
    public static bool Empty<T>(this IEnumerable<T> target) => !target.Any();
    
    public static bool Empty<T>(this List<T> target) => target.Count == 0;
    
    public static bool Empty<T>(this Queue<T> target) => target.Count == 0;
    
    public static bool Empty(this string target) => target.Length == 0;
    
    public static bool NotEmpty<T>(this T[] target) => target.Length > 0;
    
    public static bool NotEmpty<T>(this IEnumerable<T> target) => target.Any();
    
    public static bool NotEmpty<T>(this List<T> target) => target.Count > 0;
    
    public static bool NotEmpty<T>(this Queue<T> target) => target.Count > 0;
    
    public static bool NotEmpty(this string target) => target.Length > 0;
}