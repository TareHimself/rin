namespace Rin.Engine;

public interface ICache<TKey, TValue>
{
    public IEnumerable<TKey> Keys { get; }
    public TValue Get(TKey key);
    public bool Has(TKey key);
    public void Put(TKey key, TValue value);
}