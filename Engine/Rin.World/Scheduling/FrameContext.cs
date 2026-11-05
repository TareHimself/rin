namespace Rin.World.Scheduling;

public readonly struct FrameContext(World world, float deltaSeconds, float interpolationAlpha)
{
    public World World { get; } = world;

    /// <summary>
    ///     The frame delta scaled by <see cref="World.TimeScale" />.
    /// </summary>
    public float DeltaSeconds { get; } = deltaSeconds;

    public float InterpolationAlpha { get; } = interpolationAlpha;
}
