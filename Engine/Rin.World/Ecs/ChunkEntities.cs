using Rin.World.Ecs;

namespace Rin.World;

public readonly struct ChunkEntities
{
    private readonly int[] _ids;
    private readonly EntityTable _entities;

    internal ChunkEntities(int[] ids, int length, EntityTable entities)
    {
        _ids = ids;
        Length = length;
        _entities = entities;
    }

    public int Length { get; }

    public Entity this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)Length, nameof(index));
            return _entities.HandleOf(_ids[index]);
        }
    }
}
