using System.Numerics;
using Rin.World;
using Rin.World.Physics;
using Rin.World.Scripting;
using Rin.World.Tasks;

namespace Examples.Scripting;

/// <summary>
///     A timed loop: <see cref="TaskContext.Seconds" /> between drops, at positions from <see cref="World.Random" />, the
///     simulation's seeded random numbers, so every run drops the crates in the same places.
/// </summary>
public sealed class CrateDropper(Arena arena, float intervalSeconds, float halfWidth) : Script
{
    private const float DropHeight = 12f;
    private const float CrateLifetimeSeconds = 15f;

    protected override void Start() => Tasks.Run(DropForever);

    protected override void Restore() => Tasks.Run(DropForever);

    private async GameTask DropForever(TaskContext ctx)
    {
        while (true)
        {
            await ctx.Seconds(intervalSeconds);
            var x = World.Random.NextFloat(-halfWidth, halfWidth);
            var z = World.Random.NextFloat(-halfWidth, halfWidth);
            var crate = arena.SpawnBody(new Vector3(x, DropHeight, z), new Vector3(1.5f), arena.Crate,
                PhysicsState.Simulated);
            arena.Scripts.Attach(crate, new Lifetime(CrateLifetimeSeconds));
            ScriptLog.Write(this, $"dropped a crate at ({x:F1}, {z:F1})");
        }
    }
}
