using System.Numerics;
using Rin.Core.Views;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Physics;
using Rin.World.Scripting;
using Rin.World.Tasks;
using Rin.World.Transforms;

namespace Examples.Scripting;

/// <summary>
///     Racing a timer against an event: <see cref="TaskContext.WhenAny" /> finishes with whichever of the fuse and a
///     <see cref="Defuse" /> event comes first, and cancels the other one.
/// </summary>
public sealed class Bomb(Arena arena) : Script
{
    private const float FuseSeconds = 4f;
    private const float MaxGlow = 150f;
    private const int DebrisCount = 14;
    private const float DebrisSpeed = 12f;
    private const float DebrisLifetimeSeconds = 6f;

    private Entity _glow;

    protected override void Start()
    {
        _glow = arena.SpawnLight(Entity, new Vector3(0f, 2.5f, 0f), Color.FromRgb(1f, 0.3f, 0.1f));
        Tasks.Run(WaitForFuseOrDefuse);
    }

    private async GameTask WaitForFuseOrDefuse(TaskContext ctx)
    {
        ScriptLog.Write(this, $"armed, {FuseSeconds} s fuse, press F to defuse");
        var winner = await ctx.WhenAny(BurnFuse(ctx), ctx.Event<Defuse>(Entity));

        if (winner == 0)
        {
            ScriptLog.Write(this, "the fuse won the race: boom");
            Explode();
        }
        else
        {
            ScriptLog.Write(this, "the Defuse event won the race, the fuse task was cancelled");
            await ShrinkAway(ctx);
        }

        World.Despawn(Entity);
    }

    // Pulses faster and glows brighter as the fuse burns down.
    private async GameTask BurnFuse(TaskContext ctx)
    {
        var restScale = World.Get<LocalTransform>(Entity).Scale;
        var ticks = (int)MathF.Ceiling(FuseSeconds / World.TickInterval);
        var phase = 0f;
        for (var tick = 0; tick < ticks; tick++)
        {
            var burnt = tick / (float)ticks;
            phase += World.TickInterval * MathF.Tau * (1f + 5f * burnt);
            World.SetScale(Entity, restScale * (1f + 0.2f * MathF.Sin(phase)));
            World.Get<PointLight>(_glow).Radiance = MaxGlow * burnt;
            await ctx.NextTick();
        }
    }

    private async GameTask ShrinkAway(TaskContext ctx)
    {
        World.Get<PointLight>(_glow).Color = Color.FromRgb(0.2f, 1f, 0.3f);
        var startScale = World.Get<LocalTransform>(Entity).Scale;
        var steps = (int)MathF.Ceiling(0.5f / World.TickInterval);
        for (var step = 1; step <= steps; step++)
        {
            World.SetScale(Entity, startScale * (1f - step / (float)steps));
            await ctx.NextTick();
        }
    }

    private void Explode()
    {
        var center = World.Get<LocalTransform>(Entity).Position;
        var random = World.Random;
        for (var i = 0; i < DebrisCount; i++)
        {
            var direction = Vector3.Normalize(new Vector3(
                random.NextFloat(-1f, 1f),
                random.NextFloat(0.5f, 1.5f),
                random.NextFloat(-1f, 1f)));
            var debris = arena.SpawnBody(center + direction, new Vector3(0.5f), arena.Debris, PhysicsState.Simulated,
                direction * DebrisSpeed);
            arena.Scripts.Attach(debris, new Lifetime(DebrisLifetimeSeconds));
        }
    }
}
