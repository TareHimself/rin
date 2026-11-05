using Rin.World.Ecs;
using FrifloChunkEntities = Friflo.Engine.ECS.ChunkEntities;

namespace Rin.World;

public readonly struct ChunkEntities
{
    private readonly FrifloChunkEntities _entities;

    internal ChunkEntities(FrifloChunkEntities entities) => _entities = entities;

    public int Length => _entities.Length;

    public Entity this[int index] => _entities.EntityAt(index).ToHandle();
}
