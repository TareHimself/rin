namespace Rin.Framework;

public abstract class Factory<TValue, TKey, TInternalKey> : IDisposable where TInternalKey : notnull
{
    private readonly Dictionary<TInternalKey, TValue> _data = [];

    private readonly Lock _lock = new();
    public abstract void Dispose();

    public IEnumerable<KeyValuePair<TInternalKey, TValue>> GetData()
    {
        lock (_lock)
        {
            return _data.ToArray();
        }
    }

    public virtual TValue Get(TKey key)
    {
        var internalKey = ToInternalKey(key);

        lock (_lock)
        {
            if (!_data.ContainsKey(internalKey)) _data.Add(internalKey, CreateNew(key, internalKey));

            return _data[internalKey];
        }
    }

    protected abstract TInternalKey ToInternalKey(TKey key);
    protected abstract TValue CreateNew(TKey key, TInternalKey internalKey);
}