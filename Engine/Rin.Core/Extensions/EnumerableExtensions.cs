namespace Rin.Core.Extensions;

public static class EnumerableExtensions
{
    extension<TFirst>(IEnumerable<TFirst> first)
    {
        public IEnumerable<(TFirst First, TSecond Second, TThird Third, TFourth Fourth, TFifth Fifth)>
            Zip<TSecond, TThird, TFourth, TFifth>(IEnumerable<TSecond> second,
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
    }

    public static IEnumerable<T> AsReversed<T>(this T[] target)
    {
        for (var i = target.Length - 1; i > -1; i--) yield return target[i];
    }

    public static IEnumerable<T> AsReversed<T>(this IEnumerable<T> target)
    {
        if (target is List<T> asList)
        {
            for (var i = asList.Count - 1; i > -1; i--)
            {
                if (asList.Count <= i) continue;
                yield return asList[i];
            }

            yield break;
        }

        if (target is LinkedList<T> asLinkedList)
        {
            for (var node = asLinkedList.Last; node != null; node = node.Previous) yield return node.Value;

            yield break;
        }

        var asArray = target.ToArray();
        for (var i = asArray.Length - 1; i > -1; i--) yield return asArray[i];
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

    public static bool Empty<T>(this HashSet<T> target)
    {
        return target.Count == 0;
    }

    public static bool Empty<TKey, TValue>(this Dictionary<TKey, TValue> target) where TKey : notnull
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

    public static bool NotEmpty<T>(this HashSet<T> target)
    {
        return target.Count > 0;
    }

    public static bool NotEmpty<TKey, TValue>(this Dictionary<TKey, TValue> target) where TKey : notnull
    {
        return target.Count > 0;
    }

    public static bool NotEmpty(this string target)
    {
        return target.Length > 0;
    }
}