namespace Rin.Core.Shared.Pooling;

/// <summary>
///     Keeps spare objects so one that is finished with can be used again. Safe to take on one thread and return
///     on another. When an object is finished with is up to its owner.
/// </summary>
public sealed class Recycler<T>(Func<T> create, int maxSpares = 8) where T : class, IResettable
{
    private readonly Stack<T> _spares = new();

    public T Take()
    {
        lock (_spares)
        {
            if (_spares.TryPop(out var spare)) return spare;
        }

        return create();
    }

    /// <summary>
    ///     Resets the object and keeps it as a spare if there is room.
    /// </summary>
    public void Return(T item)
    {
        item.Reset();
        lock (_spares)
        {
            if (_spares.Count < maxSpares) _spares.Push(item);
        }
    }
}
