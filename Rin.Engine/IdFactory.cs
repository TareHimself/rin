using JetBrains.Annotations;

namespace Rin.Engine;

public class IdFactory
{
    private readonly Queue<int> _freeIds = [];
    private readonly object _lock = new();

    [PublicAPI] public int CurrentId { get; private set; }

    [PublicAPI]
    public int NewId()
    {
        lock (_lock)
        {
            if (_freeIds.Count != 0) return _freeIds.Dequeue();

            var id = CurrentId++;

            return id;
        }
    }

    [PublicAPI]
    public void FreeId(int id)
    {
        lock (_lock)
        {
            _freeIds.Enqueue(id);
        }
    }
}