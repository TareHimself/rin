namespace Rin.World.Scheduling;

public abstract class TickSystem
{
    public abstract void Tick(in TickContext context);
}
