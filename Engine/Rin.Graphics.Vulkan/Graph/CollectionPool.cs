namespace Rin.Graphics.Vulkan.Graph;

/// <summary>
///     Hands out collections that keep their capacity, for per-frame state that is rebuilt every frame.
/// </summary>
internal sealed class CollectionPool<TCollection, TItem> where TCollection : ICollection<TItem>, new()
{
    private readonly Stack<TCollection> _free = [];

    public TCollection Rent()
    {
        return _free.TryPop(out var collection) ? collection : new TCollection();
    }

    public void Return(TCollection collection)
    {
        collection.Clear();
        _free.Push(collection);
    }

    /// <summary>
    ///     Returns every collection the dictionary holds and empties the dictionary.
    /// </summary>
    public void ReturnAll<TKey>(Dictionary<TKey, TCollection> owners) where TKey : notnull
    {
        foreach (var collection in owners.Values) Return(collection);
        owners.Clear();
    }
}
