namespace Rin.World.Ecs.Scheduling;

public abstract class FrameSystem
{
    public abstract void Update(in FrameContext context);
}
