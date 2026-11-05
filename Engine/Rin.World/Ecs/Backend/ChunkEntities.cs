using Rin.World.Ecs.Backend;
using FrifloChunkEntities = Friflo.Engine.ECS.ChunkEntities;

namespace Rin.World.Ecs;

public readonly struct ChunkEntities
{
    private readonly FrifloChunkEntities _entities;

    internal ChunkEntities(FrifloChunkEntities entities) => _entities = entities;

    public int Length => _entities.Length;

    public Entity this[int index] => _entities.EntityAt(index).ToHandle();
}
