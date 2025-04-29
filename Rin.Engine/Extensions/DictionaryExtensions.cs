namespace Rin.Engine.Extensions;

public static class DictionaryExtensions
{
    public static void RemoveWhere<TKey, TValue>(this Dictionary<TKey, TValue> target,
        Func<TKey, TValue, bool> predicate) where TKey : notnull
    {
        foreach (var key in target.Keys.ToArray())
            if (predicate(key, target[key]))
                target.Remove(key);
    }

    public static void RemoveWhere<TKey, TValue>(this Dictionary<TKey, TValue> target,
        Func<TKey, bool> predicate) where TKey : notnull
    {
        foreach (var key in target.Keys.ToArray())
            if (predicate(key))
                target.Remove(key);
    }
    
}