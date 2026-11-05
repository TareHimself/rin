using Rin.World.Scheduling;

namespace Rin.World.Physics;

internal sealed class PhysicsStep(IPhysicsSystem physicsSystem) : TickSystem
{
    public override void Tick(in TickContext context) => physicsSystem.Update(context.DeltaSeconds);
}
