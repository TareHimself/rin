using System.Numerics;
using Rin.World;
using Rin.World.Scripting;
using Rin.World.Tasks;
using Rin.World.Transforms;

namespace Examples.Scripting;

/// <summary>
///     A sequence: <see cref="TaskContext.Until" /> waits for the patroller, then timed steps open the door, hold it and
///     close it. Whether it is open lives in <see cref="DoorState" />, so <see cref="Restore" /> can restart the loop.
/// </summary>
public sealed class SlidingDoor(Entity patroller, Vector3 closedPosition, Vector3 openPosition) : Script
{
    private const float TriggerDistance = 4f;
    private const float SlideSeconds = 1f;
    private const float HoldOpenSeconds = 2f;

    protected override void Start() => Tasks.Run(OpenAndClose);

    // Every task was cancelled by the restore and task locals are gone, so start again from the saved DoorState.
    protected override void Restore() => Tasks.Run(OpenAndClose);

    private async GameTask OpenAndClose(TaskContext ctx)
    {
        while (true)
        {
            if (!World.Get<DoorState>(Entity).IsOpen)
            {
                ScriptLog.Write(this, "closed, waiting until the patroller is near");
                await ctx.Until(IsPatrollerNear);
                ScriptLog.Write(this, $"patroller is near, opening over {SlideSeconds} s");
                await SlideTo(ctx, openPosition);
                World.Get<DoorState>(Entity).IsOpen = true;
            }

            ScriptLog.Write(this, $"open, closing in {HoldOpenSeconds} s");
            await ctx.Seconds(HoldOpenSeconds);
            ScriptLog.Write(this, "closing");
            await SlideTo(ctx, closedPosition);
            World.Get<DoorState>(Entity).IsOpen = false;
        }
    }

    // A GameTask can await another one: this moves one step per tick until the door reaches the target.
    private async GameTask SlideTo(TaskContext ctx, Vector3 target)
    {
        var start = World.Get<LocalTransform>(Entity).Position;
        var steps = (int)MathF.Ceiling(SlideSeconds / World.TickInterval);
        for (var step = 1; step <= steps; step++)
        {
            World.SetPosition(Entity, Vector3.Lerp(start, target, step / (float)steps));
            await ctx.NextTick();
        }
    }

    private bool IsPatrollerNear()
    {
        var patrollerPosition = World.Get<LocalTransform>(patroller).Position;
        return Vector3.Distance(patrollerPosition, closedPosition) < TriggerDistance;
    }
}
