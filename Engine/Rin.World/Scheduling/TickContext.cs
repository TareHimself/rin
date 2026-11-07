using Rin.Core.Shared.Math;

namespace Rin.World.Scheduling;

public readonly struct TickContext(World world, long tick, float deltaSeconds)
{
    public World World { get; } = world;

    public long Tick { get; } = tick;

    /// <summary>
    ///     Always the world's <see cref="World.TickInterval" />, never the frame delta.
    /// </summary>
    public float DeltaSeconds { get; } = deltaSeconds;

    public DeterministicRandom Random => World.Random;
}
