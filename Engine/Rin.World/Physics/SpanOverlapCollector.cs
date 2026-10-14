namespace Rin.World.Physics;

/// <summary>
///     Writes overlapping bodies into a caller's buffer and stops the query when it is full. Holds a pointer rather
///     than a span because the physics backends constrain their handler generics to plain structs.
/// </summary>
internal unsafe struct SpanOverlapCollector(PhysicsBodyHandle* buffer, int capacity) : IPhysicsOverlapHandler
{
    public int Count { get; private set; }

    public bool OnOverlap(PhysicsBodyHandle body)
    {
        buffer[Count++] = body;
        return Count < capacity;
    }
}
