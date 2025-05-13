namespace Rin.Engine.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<(TFirst First, TSecond Second, TThird Third, TFourth Fourth)>
        Zip<TFirst, TSecond, TThird, TFourth>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second,
            IEnumerable<TThird> third, IEnumerable<TFourth> fourth)
    {
        using var e1 = first.GetEnumerator();
        using var e2 = second.GetEnumerator();
        using var e3 = third.GetEnumerator();
        using var e4 = fourth.GetEnumerator();

        while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext() && e4.MoveNext())
            yield return (e1.Current, e2.Current, e3.Current, e4.Current);
    }


    public static IEnumerable<(TFirst First, TSecond Second, TThird Third, TFourth Fourth, TFifth Fifth)>
        Zip<TFirst, TSecond, TThird, TFourth, TFifth>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second,
            IEnumerable<TThird> third, IEnumerable<TFourth> fourth, IEnumerable<TFifth> fifth)
    {
        using var e1 = first.GetEnumerator();
        using var e2 = second.GetEnumerator();
        using var e3 = third.GetEnumerator();
        using var e4 = fourth.GetEnumerator();
        using var e5 = fifth.GetEnumerator();

        while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext() && e4.MoveNext() && e5.MoveNext())
            yield return (e1.Current, e2.Current, e3.Current, e4.Current, e5.Current);
    }

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
                for (var i = asList.Count - 1; i > -1; i--)
                {
                    if (asList.Count <= i) continue;
                    yield return asList[i];
                }
        }

        {
            if (target is LinkedList<T> asLinkedList)
            {
                var el = asLinkedList.Last;
                while (el != null)
                {
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

    public static ulong ComputeByteSize<T>(this IEnumerable<T> target) where T : unmanaged
    {
        return Utils.ByteSizeOf<T>(target.Count());
    }

    public static List<T> UpdateIndex<T>(this List<T> target, int index, Func<T, T> updater)
    {
        target[index] = updater(target[index]);
        return target;
    }

    public static T? TryGet<T>(this T[] target, int index) where T : class?
    {
        if (target.Length <= index || index < 0) return null;

        return target[index];
    }


    public static bool Empty<T>(this T[] target)
    {
        return target.Length == 0;
    }

    public static bool Empty<T>(this IEnumerable<T> target)
    {
        return !target.Any();
    }

    public static bool Empty<T>(this List<T> target)
    {
        return target.Count == 0;
    }

    public static bool Empty<T>(this Queue<T> target)
    {
        return target.Count == 0;
    }

    public static bool Empty(this string target)
    {
        return target.Length == 0;
    }

    public static bool NotEmpty<T>(this T[] target)
    {
        return target.Length > 0;
    }

    public static bool NotEmpty<T>(this IEnumerable<T> target)
    {
        return target.Any();
    }

    public static bool NotEmpty<T>(this List<T> target)
    {
        return target.Count > 0;
    }

    public static bool NotEmpty<T>(this Queue<T> target)
    {
        return target.Count > 0;
    }

    public static bool NotEmpty(this string target)
    {
        return target.Length > 0;
    }

    public static Queue<T> ToQueue<T>(this IEnumerable<T> enumerable)
    {
        return new Queue<T>(enumerable);
    }
}