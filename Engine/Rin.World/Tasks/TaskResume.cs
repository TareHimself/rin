using Rin.World.Scheduling;

namespace Rin.World.Tasks;

internal sealed class TaskResume(GameTaskScheduler scheduler) : TickSystem
{
    public override void Tick(in TickContext context) => scheduler.ResumeDue();
}
