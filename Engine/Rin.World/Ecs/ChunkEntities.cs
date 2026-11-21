using Rin.World.Ecs;

namespace Rin.World;

public readonly struct ChunkEntities
{
    private readonly int[] _ids;
    private readonly EntityIdAllocator _allocator;

    internal ChunkEntities(int[] ids, int length, EntityIdAllocator allocator)
    {
        _ids = ids;
        Length = length;
        _allocator = allocator;
    }

    public int Length { get; }

    public Entity this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)Length, nameof(index));
            return _allocator.HandleOf(_ids[index]);
        }
    }
}
