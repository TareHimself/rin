using System.Numerics;
using Rin.Core.Views;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Scripting;
using Rin.World.Tasks;
using Rin.World.Transforms;

namespace Examples.Scripting;

/// <summary>
///     Cancellation: despawning the entity mid-charge throws <see cref="GameTaskCanceledException" /> at the pending
///     await, so the task's <c>catch</c> and <c>finally</c> run, and then <see cref="Stop" />.
/// </summary>
public sealed class ChargingLight(Arena arena) : Script
{
    private const float ChargeSeconds = 6f;
    private const float FlashSeconds = 0.3f;
    private const float MaxGlow = 150f;
    private const float FlashGlow = 800f;
    private const float Width = 1.2f;
    private const float MinHeight = 0.4f;
    private const float MaxHeight = 3f;

    private Entity _light;

    protected override void Start()
    {
        _light = arena.SpawnLight(Entity, new Vector3(0f, 1.5f, 0f), Color.FromRgb(0.3f, 0.6f, 1f));
        Tasks.Run(ChargeAndFlash);
    }

    protected override void Stop() => ScriptLog.Write(this, "Stop: runs after the task's catch and finally");

    private async GameTask ChargeAndFlash(TaskContext ctx)
    {
        var charge = 0f;
        try
        {
            while (true)
            {
                ScriptLog.Write(this, $"charging for {ChargeSeconds} s, press K to despawn it mid-charge");
                var ticks = (int)MathF.Ceiling(ChargeSeconds / World.TickInterval);
                for (var tick = 1; tick <= ticks; tick++)
                {
                    charge = tick / (float)ticks;
                    ShowCharge(charge, MaxGlow * charge);
                    await ctx.NextTick();
                }

                ScriptLog.Write(this, "fully charged, flash");
                ShowCharge(1f, FlashGlow);
                await ctx.Seconds(FlashSeconds);
                charge = 0f;
            }
        }
        catch (GameTaskCanceledException e)
        {
            ScriptLog.Write(this, $"catch: cancelled at {charge:P0} charge, Reason = {e.Reason}");
        }
        finally
        {
            ScriptLog.Write(this, $"finally: always runs, ctx.CancelReason = {ctx.CancelReason}");
        }
    }

    // A pillar that grows from the floor as it charges.
    private void ShowCharge(float charge, float glow)
    {
        var height = MinHeight + (MaxHeight - MinHeight) * charge;
        ref var transform = ref World.Get<LocalTransform>(Entity);
        transform.Position = transform.Position with { Y = height / 2f };
        transform.Scale = new Vector3(Width, height, Width) / 2f;
        World.Get<PointLight>(_light).Radiance = glow;
    }
}
