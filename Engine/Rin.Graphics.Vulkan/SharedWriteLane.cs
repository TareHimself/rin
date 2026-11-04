namespace Rin.Graphics.Vulkan;

/// <summary>
///     Every queued <see cref="SharedWrite" /> in enqueue order, which is the order they must run in.
/// </summary>
internal sealed class SharedWriteLane
{
    private readonly List<SharedWrite> _items = [];

    public Lock Sync { get; } = new();

    public int Count
    {
        get
        {
            lock (Sync)
            {
                return _items.Count;
            }
        }
    }

    public void Append(SharedWrite write)
    {
        lock (Sync)
        {
            _items.Add(write);
        }
    }

    public SharedWrite? First()
    {
        lock (Sync)
        {
            return _items.Count > 0 ? _items[0] : null;
        }
    }

    public void Peek(List<SharedWrite> into)
    {
        lock (Sync)
        {
            into.AddRange(_items);
        }
    }

    public void MarkTaken(SharedWrite write)
    {
        lock (Sync)
        {
            if (write.Taken) return;
            write.Taken = true;
            _items.Remove(write);
        }
    }

    public void TakeAll(List<SharedWrite> into)
    {
        lock (Sync)
        {
            into.AddRange(_items);
            _items.Clear();
        }
    }
}
