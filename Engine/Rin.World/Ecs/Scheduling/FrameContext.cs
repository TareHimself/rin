namespace Rin.World.Ecs.Scheduling;

public readonly struct FrameContext(EntityWorld world, float deltaSeconds, float interpolationAlpha)
{
    public EntityWorld World { get; } = world;

    /// <summary>
    ///     The frame delta scaled by <see cref="EntityWorld.TimeScale" />.
    /// </summary>
    public float DeltaSeconds { get; } = deltaSeconds;

    public float InterpolationAlpha { get; } = interpolationAlpha;
}
