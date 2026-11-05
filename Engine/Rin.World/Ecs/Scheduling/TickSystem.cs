namespace Rin.World.Ecs.Scheduling;

public abstract class TickSystem
{
    public abstract void Tick(in TickContext context);
}
