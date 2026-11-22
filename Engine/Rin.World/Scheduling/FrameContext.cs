namespace Rin.World.Scheduling;

public readonly struct FrameContext(World world, float deltaSeconds, float interpolationAlpha, int ticksRun)
{
    public World World { get; } = world;

    /// <summary>
    ///     The frame delta scaled by <see cref="World.TimeScale" />.
    /// </summary>
    public float DeltaSeconds { get; } = deltaSeconds;

    public float InterpolationAlpha { get; } = interpolationAlpha;

    /// <summary>
    ///     The fixed ticks run just before this frame: 0 while paused, or when the frame came before the next tick was due.
    /// </summary>
    public int TicksRun { get; } = ticksRun;
}
