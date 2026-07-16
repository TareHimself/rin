namespace Rin.Core.Extensions;

public static class DictionaryExtensions
{
    extension<TKey, TValue>(Dictionary<TKey, TValue> target) where TKey : notnull
    {
        public void RemoveWhere(Func<TKey, TValue, bool> predicate)
        {
            foreach (var key in target.Keys.ToArray())
                if (predicate(key, target[key]))
                    target.Remove(key);
        }

        public void RemoveWhere(Func<TKey, bool> predicate)
        {
            foreach (var key in target.Keys.ToArray())
                if (predicate(key))
                    target.Remove(key);
        }
    }
}