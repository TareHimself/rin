using Rin.World.Ecs;
using FrifloChunkEntities = Friflo.Engine.ECS.ChunkEntities;

namespace Rin.World;

public readonly struct ChunkEntities
{
    private readonly FrifloChunkEntities _entities;
    private readonly EntityIdAllocator _ids;

    internal ChunkEntities(FrifloChunkEntities entities, EntityIdAllocator ids)
    {
        _entities = entities;
        _ids = ids;
    }

    public int Length => _entities.Length;

    public Entity this[int index] => _ids.HandleOf(_entities.Ids[index]);
}
