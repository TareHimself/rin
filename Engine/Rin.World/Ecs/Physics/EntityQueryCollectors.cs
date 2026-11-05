using Rin.World.Physics;

namespace Rin.World.Ecs.Physics;

/// <summary>
///     Writes hits with their entity into a caller's buffer and stops the query when it is full. Holds a pointer for the
///     same reason as <see cref="SpanHitCollector" />.
/// </summary>
internal unsafe struct EntityHitCollector(EntityHit* buffer, int capacity, BodyEntityMap bodies) : IPhysicsHitHandler
{
    public int Count { get; private set; }

    public bool OnHit(in RayCastResult hit)
    {
        buffer[Count++] = new EntityHit(bodies.Find(hit.Body), hit);
        return Count < capacity;
    }
}

/// <summary>
///     Writes the entities of overlapping bodies into a caller's buffer, skipping bodies without one.
/// </summary>
internal unsafe struct EntityOverlapCollector(Entity* buffer, int capacity, BodyEntityMap bodies) : IPhysicsOverlapHandler
{
    public int Count { get; private set; }

    public bool OnOverlap(PhysicsBodyHandle body)
    {
        var entity = bodies.Find(body);
        if (!entity.IsValid) return true;
        buffer[Count++] = entity;
        return Count < capacity;
    }
}
