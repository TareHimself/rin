using Rin.World.Ecs.Scheduling;

namespace Rin.World.Ecs.Tasks;

internal sealed class TaskResume(GameTaskScheduler scheduler) : TickSystem
{
    public override void Tick(in TickContext context) => scheduler.ResumeDue();
}
