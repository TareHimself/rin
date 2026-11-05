using Rin.World.Ecs.Scheduling;
using Rin.World.Physics;

namespace Rin.World.Ecs.Physics;

internal sealed class PhysicsStep(IPhysicsSystem physicsSystem) : TickSystem
{
    public override void Tick(in TickContext context) => physicsSystem.Update(context.DeltaSeconds);
}
