using System.Numerics;
using Rin.World.Scheduling;
using Rin.World.Scripting;
using Rin.World.Transforms;

namespace Examples.Scripting;

/// <summary>
///     Per-tick logic: <see cref="Tick" /> runs once every fixed tick, like a classic Update method.
/// </summary>
public sealed class Patroller(Vector3 center, float halfDistance, float secondsPerLap) : Script
{
    protected override void Tick(in TickContext context)
    {
        // Time from the tick index, not the frame clock, so every run moves the same way.
        var seconds = context.Tick * context.DeltaSeconds;
        var x = MathF.Sin(seconds / secondsPerLap * MathF.Tau) * halfDistance;
        World.SetPosition(Entity, center + new Vector3(x, 0f, 0f));
    }
}
