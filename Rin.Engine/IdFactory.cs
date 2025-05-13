using JetBrains.Annotations;

namespace Rin.Engine;

public class IdFactory
{
    private readonly Queue<int> _freeIds = [];
    private readonly Lock _lock = new();

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
    public int NewId(out bool isNew)
    {
        lock (_lock)
        {
            if (_freeIds.Count != 0)
            {
                isNew = false;
                return _freeIds.Dequeue();
            }

            var id = CurrentId++;

            isNew = true;
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

    public bool IsFree(int id)
    {
        if (id > CurrentId) return true;
        lock (_freeIds)
        {
            return _freeIds.Contains(id);
        }
    }
}