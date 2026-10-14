namespace Rin.World.Physics;

/// <summary>
///     Writes hits into a caller's buffer and stops the query when it is full. Holds a pointer rather than a
///     span because the physics backends constrain their handler generics to plain structs.
/// </summary>
internal unsafe struct SpanHitCollector(RayCastResult* buffer, int capacity) : IPhysicsHitHandler
{
    public int Count { get; private set; }

    public bool OnHit(in RayCastResult hit)
    {
        buffer[Count++] = hit;
        return Count < capacity;
    }
}
