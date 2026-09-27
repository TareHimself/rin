namespace Rin.Graphics.Vulkan.Images;

// Lock-free reads: chunks never move once published; callers serialize writes.
internal sealed class ResourceSlots<T> where T : BindlessResource
{
    private readonly T?[]?[] _chunks;
    private readonly int _chunkShift;
    private readonly uint _chunkMask;
    private int _count;

    public ResourceSlots(int chunkSize, int maxChunks = 1)
    {
        if (!int.IsPow2(chunkSize)) throw new ArgumentOutOfRangeException(nameof(chunkSize), "must be a power of two");
        _chunks = new T?[]?[maxChunks];
        _chunkShift = int.Log2(chunkSize);
        _chunkMask = (uint)chunkSize - 1;
    }

    public int Capacity => _chunks.Length << _chunkShift;

    public int Count => Volatile.Read(ref _count);

    public T? Get(uint id)
    {
        var chunkIndex = id >> _chunkShift;
        if (chunkIndex >= (uint)_chunks.Length) return null;
        var chunk = Volatile.Read(ref _chunks[chunkIndex]);
        return chunk is null ? null : Volatile.Read(ref chunk[id & _chunkMask]);
    }

    public void Set(uint id, T value)
    {
        var chunkIndex = id >> _chunkShift;
        if (chunkIndex >= (uint)_chunks.Length)
            throw new InvalidOperationException($"{typeof(T).Name} slots exhausted (capacity {Capacity})");

        var chunk = _chunks[chunkIndex];
        if (chunk is null)
        {
            chunk = new T?[_chunkMask + 1];
            Volatile.Write(ref _chunks[chunkIndex], chunk);
        }

        Volatile.Write(ref chunk[id & _chunkMask], value);
        if (id >= _count) Volatile.Write(ref _count, (int)id + 1);
    }
}
