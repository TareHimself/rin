using System.Numerics;
using JetBrains.Annotations;

namespace Rin.Core.Shared;

public class IdFactory<T> where T : unmanaged, INumber<T>
{
    private readonly Queue<T> _freeIds = [];
    private readonly Lock _lock = new();

    [PublicAPI] public T CurrentId { get; private set; }

    [PublicAPI]
    public T NewId()
    {
        lock (_lock)
        {
            if (_freeIds.Count != 0) return _freeIds.Dequeue();

            var id = CurrentId++;

            return id;
        }
    }

    [PublicAPI]
    public T NewId(out bool isNew)
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
    public void FreeId(T id)
    {
        lock (_lock)
        {
            _freeIds.Enqueue(id);
        }
    }

    public bool IsFree(T id)
    {
        if (id > CurrentId) return true;
        lock (_freeIds)
        {
            return _freeIds.Contains(id);
        }
    }
}

public class IdFactory : IdFactory<int> {}