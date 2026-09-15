using Rin.World.Physics;

namespace Rin.World.Components;

public interface IPhysicsComponent : IWorldComponent
{
    public void PrePhysicsUpdate();
    public void ProcessHit(IPhysicsBody body, RayCastResult result);
}