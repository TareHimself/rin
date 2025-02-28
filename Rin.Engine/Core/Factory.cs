using System.Collections.Concurrent;

namespace Rin.Engine.Core;

public abstract class Factory<TValue, TKey, TInternalKey> : Disposable where TInternalKey : notnull
{
    private readonly ConcurrentDictionary<TInternalKey, TValue> _data = [];

    public ConcurrentDictionary<TInternalKey, TValue> GetData()
    {
        return _data;
    }

    public virtual TValue Get(TKey key)
    {
        var internalKey = ToInternalKey(key);
        if (!_data.ContainsKey(internalKey)) _data.TryAdd(internalKey, CreateNew(key, internalKey));

        return _data[internalKey];
    }

    protected abstract TInternalKey ToInternalKey(TKey key);
    protected abstract TValue CreateNew(TKey key, TInternalKey internalKey);
}