namespace Rin.World.Ecs.Scheduling;

public readonly struct TickContext(EntityWorld world, long tick, float deltaSeconds)
{
    public EntityWorld World { get; } = world;

    public long Tick { get; } = tick;

    /// <summary>
    ///     Always the world's <see cref="EntityWorld.TickInterval" />, never the frame delta.
    /// </summary>
    public float DeltaSeconds { get; } = deltaSeconds;
}
